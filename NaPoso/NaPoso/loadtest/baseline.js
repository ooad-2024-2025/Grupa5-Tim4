import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const errorRate = new Rate('errors');
const httpDuration = new Trend('http_duration');

export const options = {
  stages: [
    { duration: '30s', target: 10 },   // Ramp up
    { duration: '1m', target: 10 },     // Steady state
    { duration: '30s', target: 20 },    // Peak load
    { duration: '1m', target: 20 },     // Sustained peak
    { duration: '30s', target: 0 },     // Ramp down
  ],
  thresholds: {
    http_duration: ['p(95)<500'],       // 95th percentile < 500ms
    errors: ['rate<0.1'],               // Error rate < 10%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  // Public routes
  let res = http.get(`${BASE_URL}/`);
  check(res, { 'home status 200': (r) => r.status === 200 });
  httpDuration.add(res.timings.duration);
  errorRate.add(res.status !== 200);

  res = http.get(`${BASE_URL}/health/live`);
  check(res, { 'health/live status 200': (r) => r.status === 200 });
  httpDuration.add(res.timings.duration);
  errorRate.add(res.status !== 200);

  res = http.get(`${BASE_URL}/health/ready`);
  check(res, { 'health/ready status 200': (r) => r.status === 200 });
  httpDuration.add(res.timings.duration);
  errorRate.add(res.status !== 200);

  // Login page (public GET)
  res = http.get(`${BASE_URL}/Identity/Account/Login`);
  check(res, { 'login page status 200': (r) => r.status === 200 });
  httpDuration.add(res.timings.duration);
  errorRate.add(res.status !== 200);

  sleep(1);
}
