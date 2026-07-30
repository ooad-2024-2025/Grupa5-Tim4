// NaPoso — Site JavaScript
(function () {
  'use strict';

  // ── Theme Toggle ──
  const THEME_KEY = 'naposo-theme';

  function getPreferredTheme() {
    var stored = localStorage.getItem(THEME_KEY);
    if (stored === 'system' || !stored) {
      return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    return stored;
  }

  function setTheme(theme) {
    var resolved = theme;
    if (theme === 'system') {
      resolved = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    document.documentElement.setAttribute('data-theme', resolved);
    localStorage.setItem(THEME_KEY, theme);
    updateToggleButtons(theme);
  }

  function updateToggleButtons(theme) {
    document.querySelectorAll('.theme-toggle-btn').forEach(function (btn) {
      var btnTheme = btn.dataset.theme;
      if (btnTheme === 'system') {
        btn.classList.toggle('active', theme === 'system');
      } else {
        btn.classList.toggle('active', btnTheme === theme);
      }
    });
  }

  // Apply theme immediately to prevent flash
  setTheme(getPreferredTheme());

  document.addEventListener('DOMContentLoaded', function () {
    // Theme toggle click handlers
    document.querySelectorAll('.theme-toggle-btn').forEach(function (btn) {
      btn.addEventListener('click', function () {
        setTheme(this.dataset.theme);
      });
    });

    // Listen for system preference changes
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
      var stored = localStorage.getItem(THEME_KEY);
      if (!stored || stored === 'system') {
        var resolved = e.matches ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', resolved);
        updateToggleButtons(stored || 'system');
      }
    });

    // ── Password Show/Hide ──
    document.querySelectorAll('.password-toggle').forEach(function (toggle) {
      toggle.addEventListener('click', function () {
        var wrapper = this.closest('.password-wrapper');
        var input = wrapper.querySelector('input');
        var isPassword = input.type === 'password';

        input.type = isPassword ? 'text' : 'password';
        this.setAttribute('aria-label', isPassword ? 'Sakrij lozinku' : 'Prikaži lozinku');
        this.querySelector('.icon-eye').style.display = isPassword ? 'none' : 'block';
        this.querySelector('.icon-eye-off').style.display = isPassword ? 'block' : 'none';
        input.focus();
      });

      // Keyboard accessibility
      toggle.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          this.click();
        }
      });
    });

    // ── Notification AJAX handler ──
    document.querySelectorAll('.notification-item').forEach(function (item) {
      item.addEventListener('click', function (e) {
        e.preventDefault();
        var notificationId = this.dataset.id;
        var self = this;

        fetch('/ObavijestKorisniku/MarkAsReadAjax', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getAntiForgeryToken()
          },
          body: 'id=' + encodeURIComponent(notificationId)
        }).then(function () {
          self.style.opacity = '0';
          self.style.transform = 'translateX(20px)';
          setTimeout(function () {
            self.remove();
            updateNotificationCount();
          }, 200);
        });
      });
    });

    function updateNotificationCount() {
      var remaining = document.querySelectorAll('.notification-item').length;
      document.querySelectorAll('.notification-count').forEach(function (badge) {
        if (remaining <= 0) {
          badge.classList.add('d-none');
        } else {
          badge.textContent = remaining;
        }
      });
      if (remaining === 0) {
        var container = document.querySelector('.notification-container');
        if (container) {
          container.innerHTML = '<span class="dropdown-item no-notifications">Nema novih obavijesti</span>';
        }
      }
    }

    function getAntiForgeryToken() {
      var el = document.querySelector('input[name="__RequestVerificationToken"]');
      return el ? el.value : '';
    }

    // ── Smooth page transitions ──
    var main = document.querySelector('main');
    if (main) {
      main.style.opacity = '0';
      main.style.transform = 'translateY(8px)';
      requestAnimationFrame(function () {
        main.style.transition = 'opacity 0.25s ease, transform 0.25s ease';
        main.style.opacity = '1';
        main.style.transform = 'translateY(0)';
      });
    }

    // ── Toast Notification System ──
    window.NaPosoToast = {
      show: function (message, type, duration) {
        type = type || 'info';
        duration = duration || 4000;
        var container = document.getElementById('toast-container');
        if (!container) {
          container = document.createElement('div');
          container.id = 'toast-container';
          container.style.cssText = 'position:fixed;top:80px;right:20px;z-index:9999;display:flex;flex-direction:column;gap:8px;';
          document.body.appendChild(container);
        }
        var toast = document.createElement('div');
        toast.className = 'naPoso-toast naPoso-toast-' + type;
        var icons = { success: 'bi-check-circle-fill', danger: 'bi-exclamation-circle-fill', warning: 'bi-exclamation-triangle-fill', info: 'bi-info-circle-fill' };
        toast.innerHTML = '<i class="bi ' + (icons[type] || icons.info) + '"></i><span>' + message + '</span>';
        toast.style.cssText = 'display:flex;align-items:center;gap:10px;padding:12px 20px;border-radius:10px;font-size:14px;font-weight:500;box-shadow:0 4px 12px rgba(0,0,0,0.15);opacity:0;transform:translateX(20px);transition:all 0.25s ease;max-width:400px;';
        var colors = { success: 'var(--color-success-light);color:var(--color-success);border:1px solid rgba(48,164,108,0.3)', danger: 'var(--color-danger-light);color:var(--color-danger);border:1px solid rgba(229,72,77,0.3)', warning: 'var(--color-warning-light);color:var(--color-warning);border:1px solid rgba(229,161,0,0.3)', info: 'var(--color-accent-light);color:var(--color-accent);border:1px solid rgba(91,95,199,0.3)' };
        toast.style.cssText += colors[type] || colors.info;
        container.appendChild(toast);
        requestAnimationFrame(function () { toast.style.opacity = '1'; toast.style.transform = 'translateX(0)'; });
        setTimeout(function () {
          toast.style.opacity = '0';
          toast.style.transform = 'translateX(20px)';
          setTimeout(function () { toast.remove(); }, 300);
        }, duration);
      }
    };
    // ── Count-Up Animation ──
    document.querySelectorAll('.stat-value').forEach(function(el) {
      var text = el.innerText.trim();
      var numMatch = text.match(/[\d\.]+/);
      if(numMatch) {
        var numStr = numMatch[0];
        var val = parseFloat(numStr);
        if(!isNaN(val) && val > 0) {
          var isFloat = numStr.includes('.');
          var startTimestamp = null;
          var duration = 1000;
          
          var step = function(timestamp) {
            if (!startTimestamp) startTimestamp = timestamp;
            var progress = Math.min((timestamp - startTimestamp) / duration, 1);
            var currentVal = progress * val;
            
            var currentStr = isFloat ? currentVal.toFixed(2) : Math.floor(currentVal).toString();
            el.innerText = text.replace(numStr, currentStr);
            
            if (progress < 1) {
              window.requestAnimationFrame(step);
            } else {
              el.innerText = text;
            }
          };
          window.requestAnimationFrame(step);
        }
      }
    });
  });
})();
