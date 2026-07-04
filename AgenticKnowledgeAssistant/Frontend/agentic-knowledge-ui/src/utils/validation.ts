export function isValidEmail(email: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
}

export function validatePassword(password: string) {
  if (password.length < 8) {
    return 'Password must be at least 8 characters';
  }

  if (!/[A-Z]/.test(password) || !/[a-z]/.test(password) || !/[0-9]/.test(password)) {
    return 'Password must include uppercase, lowercase, and number';
  }

  return '';
}

export function isValidMobileNumber(mobileNumber: string) {
  return /^[0-9+\-\s()]{7,20}$/.test(mobileNumber.trim());
}
