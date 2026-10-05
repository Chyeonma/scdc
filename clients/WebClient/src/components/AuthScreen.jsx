import React, { useEffect, useState } from 'react';
import { login, register, verifyEmail, resendVerification, forgotPassword, resetPassword } from '../api.js';
import { navigateTo } from '../navigation.js';

const titles = {
  login: 'Chào mừng trở lại!', register: 'Tạo tài khoản mới', forgot: 'Khôi phục mật khẩu',
  pending: 'Xác minh email', verify: 'Xác minh email', reset: 'Đặt lại mật khẩu',
};

export function AuthScreen({ notify, initialLink, onLinkComplete }) {
  const [mode, setMode] = useState(initialLink?.kind || 'login');
  const [token, setToken] = useState(initialLink?.token || '');
  const [email, setEmail] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState({});
  const [info, setInfo] = useState('');
  const [cooldownUntil, setCooldownUntil] = useState(0);
  const [secondsLeft, setSecondsLeft] = useState(0);

  useEffect(() => {
    const tick = () => setSecondsLeft(Math.max(0, Math.ceil((cooldownUntil - Date.now()) / 1000)));
    tick();
    const timer = setInterval(tick, 1000);
    return () => clearInterval(timer);
  }, [cooldownUntil]);

  function navigate(nextMode) {
    setToken('');
    navigateTo('/login', { replace: true });
    onLinkComplete?.();
    setMode(nextMode);
    setError('');
    setFieldErrors({});
    setInfo('');
  }

  function FieldError({ name }) {
    const messages = Object.entries(fieldErrors).find(([key]) => key.toLowerCase() === name.toLowerCase())?.[1];
    return messages ? <small className="auth-field-error" role="alert">{messages.join(' ')}</small> : null;
  }

  async function submit(event) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setSubmitting(true);
    setError('');
    setFieldErrors({});
    setInfo('');
    try {
      if (mode === 'login') {
        await login({ login: form.get('login').trim(), password: form.get('password') });
        notify?.('success', 'Đăng nhập thành công.');
      } else if (mode === 'register') {
        const address = form.get('email').trim();
        await register({ username: form.get('username').trim(), displayName: form.get('displayName').trim(),
          email: address, password: form.get('password') });
        setEmail(address);
        setMode('pending');
        setCooldownUntil(Date.now() + 60_000);
        setInfo('Tài khoản đã được tạo. Hãy mở liên kết xác minh trong email để tiếp tục.');
      } else if (mode === 'pending' || mode === 'forgot') {
        const address = form.get('email').trim();
        setEmail(address);
        if (mode === 'pending') await resendVerification(address);
        else await forgotPassword(address);
        setCooldownUntil(Date.now() + 60_000);
        setInfo('Yêu cầu đã được tiếp nhận. Nếu email này đã đăng ký và đủ điều kiện, bạn sẽ nhận được email. Hãy kiểm tra cả mục thư rác.');
      } else if (mode === 'verify') {
        await verifyEmail(token);
        navigate('login');
        setInfo('Email đã được xác minh. Bạn có thể đăng nhập.');
      } else if (mode === 'reset') {
        if (form.get('newPassword') !== form.get('confirmPassword')) {
          setFieldErrors({ confirmPassword: ['Mật khẩu xác nhận không khớp.'] });
          return;
        }
        await resetPassword(token, form.get('newPassword'));
        navigate('login');
        setInfo('Mật khẩu đã được đặt lại. Hãy đăng nhập; nếu email chưa xác minh, bạn cần hoàn tất xác minh.');
      }
    } catch (err) {
      if (err.problem?.errorCode === 'Identity.EmailNotVerified') {
        const identifier = form.get('login');
        if (identifier?.includes('@')) setEmail(identifier.trim());
        setMode('pending');
        setError('Bạn cần xác minh email trước khi đăng nhập. Nhập email đăng ký để yêu cầu liên kết mới.');
      } else if (err.problem?.errorCode === 'Identity.InvalidOrExpiredToken') {
        setToken('');
        setError('Liên kết không còn sử dụng được. Hãy yêu cầu liên kết mới.');
      } else {
        setFieldErrors(err.problem?.errors || {});
        setError(err.message || 'Không nhận được kết quả từ máy chủ. Hãy kiểm tra kết nối; với liên kết xác minh/khôi phục, bạn có thể thử đăng nhập hoặc yêu cầu liên kết mới.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  const linkMode = mode === 'verify' || mode === 'reset';
  const requestMode = mode === 'pending' || mode === 'forgot';
  return (
    <main className="auth-page">
      <section className="auth-story" aria-label="Giới thiệu SCDC">
        <a className="brand" href="/" aria-label="Trang chủ SCDC"><span className="brand__mark" aria-hidden="true">S</span>
          <div className="brand__copy"><strong>SCDC</strong><small>Simple chat, real connections.</small></div>
        </a>
        <div className="auth-story__content">
          <span className="eyebrow">YOUR COMMUNITY, ONE PLACE</span>
          <h1>Kết nối bạn bè và cộng đồng.</h1>
          <p>Một không gian để trò chuyện, chia sẻ và giữ liên lạc với những người bạn quan tâm.</p>
        </div>
        <p className="auth-story__foot">SCDC • 2026</p>
      </section>
      <section className="auth-panel"><div className="auth-card">
        <span className="eyebrow">CHÀO MỪNG ĐẾN VỚI SCDC</span>
        <h2>{titles[mode]}</h2>
        {!linkMode && <div className="auth-tabs">
          <button type="button" disabled={submitting} className={mode === 'login' ? 'is-active' : ''} onClick={() => navigate('login')}>Đăng nhập</button>
          <button type="button" disabled={submitting} className={mode === 'register' ? 'is-active' : ''} onClick={() => navigate('register')}>Đăng ký</button>
        </div>}
        {error && <div className="auth-error-banner" role="alert">{error}</div>}
        {info && <p className="auth-desc" role="status">{info}</p>}
        {mode === 'forgot' && <p className="auth-desc">Nhập email đã dùng để đăng ký SCDC. Nếu chưa có tài khoản, hãy chọn Đăng ký trước.</p>}
        {linkMode && !token && !error && <p className="auth-desc">Liên kết không còn sử dụng được. Hãy yêu cầu liên kết mới.</p>}
        {mode === 'verify' && token && <p className="auth-desc">Bấm xác minh để hoàn tất đăng ký. Chỉ mở liên kết chưa xác minh tài khoản.</p>}
        <form className="auth-form" onSubmit={submit} key={mode}>
          {(mode === 'register' || requestMode) && <label className="form-group">
            <span>EMAIL ĐĂNG KÝ</span><input name="email" type="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
            <FieldError name="email" />
          </label>}
          {mode === 'register' && <div className="form-row">
            <label className="form-group"><span>TÊN TÀI KHOẢN</span><input name="username" autoComplete="username" required placeholder="3–32 chữ, số, dấu chấm hoặc _" /><FieldError name="username" /></label>
            <label className="form-group"><span>TÊN HIỂN THỊ</span><input name="displayName" maxLength={64} required /><FieldError name="displayName" /></label>
          </div>}
          {mode === 'login' && <label className="form-group"><span>EMAIL HOẶC TÊN TÀI KHOẢN</span><input name="login" autoComplete="username" required /><FieldError name="login" /></label>}
          {(mode === 'login' || mode === 'register') && <label className="form-group">
            <span>MẬT KHẨU</span><input name="password" type="password" autoComplete={mode === 'login' ? 'current-password' : 'new-password'} minLength={mode === 'register' ? 8 : 1} maxLength={128} required />
            <FieldError name="password" />
          </label>}
          {mode === 'reset' && token && <>
            <label className="form-group"><span>MẬT KHẨU MỚI</span><input name="newPassword" type="password" autoComplete="new-password" minLength={8} maxLength={128} required /><FieldError name="newPassword" /></label>
            <label className="form-group"><span>XÁC NHẬN MẬT KHẨU</span><input name="confirmPassword" type="password" autoComplete="new-password" minLength={8} maxLength={128} required /><FieldError name="confirmPassword" /></label>
          </>}
          {(mode === 'register' || mode === 'reset' && token) && <p className="auth-desc">Mật khẩu dài 8–128 ký tự, có ít nhất một chữ và một số.</p>}
          {(!linkMode || token) && <button className="btn btn--primary btn--full btn--lg" disabled={submitting || requestMode && secondsLeft > 0}>
            {submitting ? 'Đang xử lý...' : requestMode && secondsLeft > 0 ? `Yêu cầu lại sau ${secondsLeft} giây` :
              ({ login: 'Đăng nhập', register: 'Tạo tài khoản', pending: 'Gửi lại xác minh', forgot: 'Yêu cầu khôi phục', verify: 'Xác minh email', reset: 'Đặt lại mật khẩu' })[mode]}
          </button>}
        </form>
        <div className="auth-tabs">
          {(mode === 'login' || mode === 'pending' || mode === 'reset') && <button type="button" disabled={submitting} onClick={() => navigate('forgot')}>Quên mật khẩu / yêu cầu link mới</button>}
          {(mode === 'login' || mode === 'forgot' || mode === 'verify') && <button type="button" disabled={submitting} onClick={() => navigate('pending')}>Gửi lại xác minh</button>}
          {(linkMode || requestMode) && <button type="button" disabled={submitting} onClick={() => navigate('login')}>Về đăng nhập</button>}
        </div>
      </div></section>
    </main>
  );
}
