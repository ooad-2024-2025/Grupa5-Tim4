import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const errorRate = new Rate('errors');
const homeDuration = new Trend('home_duration');
const healthLiveDuration = new Trend('health_live_duration');
const healthReadyDuration = new Trend('health_ready_duration');
const loginDuration = new Trend('login_duration');

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export const options = {
  stages: [
    { duration: '30s', target: 10 },  // Ramp up
    { duration: '1m', target: 10 },   // Steady
    { duration: '30s', target: 20 },  // Peak
    { duration: '1m', target: 20 },   // Sustained
    { duration: '30s', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    errors: ['rate<0.1'],
  },
};

export default function () {
  // Home page
  const homeRes = http.get(`${BASE_URL}/`);
  homeDuration.add(homeRes.timings.duration);
  check(homeRes, {
    'home status 200': (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(0.5);

  // Health live
  const healthLiveRes = http.get(`${BASE_URL}/health/live`);
  healthLiveDuration.add(healthLiveRes.timings.duration);
  check(healthLiveRes, {
    'health/live status 200': (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(0.5);

  // Health ready
  const healthReadyRes = http.get(`${BASE_URL}/health/ready`);
  healthReadyDuration.add(healthReadyRes.timings.duration);
  check(healthReadyRes, {
    'health/ready status 200': (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(0.5);

  // Login page
  const loginRes = http.get(`${BASE_URL}/Identity/Account/Login`);
  loginDuration.add(loginRes.timings.duration);
  check(loginRes, {
    'login status 200': (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(1);
}

export function handleSummary(data) {
  const out = {};
  out['stdout'] = JSON.stringify({
    home_p95: data.metrics.home_duration?.values?.['p(95)'],
    health_live_p95: data.metrics.health_live_duration?.values?.['p(95)'],
    health_ready_p95: data.metrics.health_ready_duration?.values?.['p(95)'],
    login_p95: data.metrics.login_duration?.values?.['p(95)'],
    error_rate: data.metrics.errors?.values?.rate,
  }, null, 2);
  return out;
}
