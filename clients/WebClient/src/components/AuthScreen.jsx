import React, { useState } from 'react';
import { login, register, verifyEmail, forgotPassword } from '../api.js';

export function AuthScreen({ notify }) {
  const [mode, setMode] = useState('login'); // 'login', 'register', 'forgot'
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');

  async function handleLogin(e) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    setSubmitting(true);
    setError('');

    try {
      await login({
        login: form.get('login').trim(),
        password: form.get('password'),
      });
      notify?.('success', 'Đăng nhập thành công.');
    } catch (err) {
      setError(err.message || 'Đăng nhập thất bại.');
    } finally {
      setSubmitting(false);
    }
  }

  async function handleRegister(e) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    setSubmitting(true);
    setError('');

    try {
      const res = await register({
        username: form.get('username').trim(),
        displayName: form.get('displayName').trim(),
        email: form.get('email').trim(),
        password: form.get('password'),
      });

      if (res?.developmentVerificationToken) {
        await verifyEmail(res.developmentVerificationToken);
        notify?.('success', 'Tài khoản đã được tạo và tự động kích hoạt. Bạn có thể đăng nhập ngay.');
      } else {
        notify?.('success', 'Tài khoản đã được tạo. Vui lòng kiểm tra email để xác thực.');
      }
      setMode('login');
    } catch (err) {
      setError(err.message || 'Đăng ký thất bại.');
    } finally {
      setSubmitting(false);
    }
  }

  async function handleForgotPassword(e) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    setSubmitting(true);
    setError('');

    try {
      await forgotPassword(form.get('email').trim());
      notify?.('success', 'Yêu cầu đặt lại mật khẩu đã được tiếp nhận. Kiểm tra email của bạn.');
      setMode('login');
    } catch (err) {
      setError(err.message || 'Gửi yêu cầu thất bại.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="auth-page">
      {/* Brand Hero Panel */}
      <section className="auth-story" aria-label="Giới thiệu SCDC">
        <div className="brand">
          <span className="brand__mark" aria-hidden="true">S</span>
          <div className="brand__copy">
            <strong>SCDC</strong>
            <small>Simple chat, real connections.</small>
          </div>
        </div>

        <div className="auth-story__content">
          <span className="eyebrow">YOUR COMMUNITY, ONE PLACE</span>
          <h1>Trò chuyện thời gian thực. Kết nối không giới hạn.</h1>
          <p>
            Nền tảng giao tiếp hiện đại với kiến trúc Modular Monolith, hỗ trợ
            Server Channels, Direct Messaging, SignalR realtime và quản lý phân quyền mạnh mẽ.
          </p>
          <div className="feature-row">
            <span>01</span>
            <p>Không gian Server & Kênh phân quyền chi tiết</p>
          </div>
          <div className="feature-row">
            <span>02</span>
            <p>Trò chuyện trực tiếp (DM) & Nhóm chat thời gian thực</p>
          </div>
          <div className="feature-row">
            <span>03</span>
            <p>Bảo mật phiên đăng nhập, JWT & Session Management</p>
          </div>
        </div>

        <p className="auth-story__foot">SCDC MODULAR MONOLITH • 2026</p>
      </section>

      {/* Auth Card Panel */}
      <section className="auth-panel">
        <div className="auth-card">
          <span className="eyebrow">CHÀO MỪNG ĐẾN VỚI SCDC</span>
          <h2>
            {mode === 'login' && 'Chào mừng trở lại!'}
            {mode === 'register' && 'Tạo tài khoản mới'}
            {mode === 'forgot' && 'Khôi phục mật khẩu'}
          </h2>

          <div className="auth-tabs">
            <button
              type="button"
              className={mode === 'login' ? 'is-active' : ''}
              onClick={() => { setMode('login'); setError(''); }}
            >
              Đăng nhập
            </button>
            <button
              type="button"
              className={mode === 'register' ? 'is-active' : ''}
              onClick={() => { setMode('register'); setError(''); }}
            >
              Đăng ký
            </button>
          </div>

          {error && <div className="auth-error-banner">{error}</div>}

          {/* Login Form */}
          {mode === 'login' && (
            <form className="auth-form" onSubmit={handleLogin}>
              <label className="form-group">
                <span>USERNAME HOẶC EMAIL</span>
                <input
                  name="login"
                  autoComplete="username"
                  placeholder="alice hoặc alice@example.local"
                  required
                  autoFocus
                />
              </label>

              <label className="form-group">
                <div className="label-with-link">
                  <span>MẬT KHẨU</span>
                  <button
                    type="button"
                    className="link-btn"
                    onClick={() => { setMode('forgot'); setError(''); }}
                  >
                    Quên mật khẩu?
                  </button>
                </div>
                <input
                  name="password"
                  type="password"
                  autoComplete="current-password"
                  placeholder="••••••••"
                  required
                />
              </label>

              <button className="btn btn--primary btn--full btn--lg" disabled={submitting}>
                {submitting ? 'Đang xác thực...' : 'Đăng nhập'}
              </button>
            </form>
          )}

          {/* Register Form */}
          {mode === 'register' && (
            <form className="auth-form" onSubmit={handleRegister}>
              <div className="form-row">
                <label className="form-group">
                  <span>USERNAME</span>
                  <input
                    name="username"
                    placeholder="mikalz"
                    pattern="[A-Za-z0-9_.]{3,32}"
                    required
                    autoFocus
                  />
                </label>
                <label className="form-group">
                  <span>TÊN HIỂN THỊ</span>
                  <input
                    name="displayName"
                    placeholder="Mikal"
                    maxLength={64}
                    required
                  />
                </label>
              </div>

              <label className="form-group">
                <span>EMAIL</span>
                <input
                  name="email"
                  type="email"
                  placeholder="you@example.com"
                  required
                />
              </label>

              <label className="form-group">
                <span>MẬT KHẨU (TỐI THIỂU 8 KÝ TỰ)</span>
                <input
                  name="password"
                  type="password"
                  placeholder="••••••••"
                  minLength={8}
                  maxLength={128}
                  required
                />
              </label>

              <button className="btn btn--primary btn--full btn--lg" disabled={submitting}>
                {submitting ? 'Đang tạo tài khoản...' : 'Tạo tài khoản'}
              </button>
            </form>
          )}

          {/* Forgot Password Form */}
          {mode === 'forgot' && (
            <form className="auth-form" onSubmit={handleForgotPassword}>
              <p className="auth-desc">Nhập địa chỉ email của bạn để nhận liên kết đặt lại mật khẩu.</p>
              <label className="form-group">
                <span>EMAIL ĐÃ ĐĂNG KÝ</span>
                <input
                  name="email"
                  type="email"
                  placeholder="you@example.com"
                  required
                  autoFocus
                />
              </label>

              <button className="btn btn--primary btn--full btn--lg" disabled={submitting}>
                {submitting ? 'Đang gửi yêu cầu...' : 'Gửi liên kết khôi phục'}
              </button>
            </form>
          )}
        </div>
      </section>
    </main>
  );
}
