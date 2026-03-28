(function () {
  function bindAuthForm(formId, endpoint, redirectPath) {
    const form = document.getElementById(formId);
    if (!form) return;

    const submitButton = form.querySelector("button[type='submit']");
    const errorEl = document.querySelector(".auth-error");

    form.addEventListener("submit", async function (event) {
      event.preventDefault();

      const emailInput = form.querySelector("input[name='email']");
      const passwordInput = form.querySelector("input[name='password']");
      const email = String(emailInput?.value || "").trim();
      const password = String(passwordInput?.value || "");

      if (!email || !password) {
        if (errorEl) errorEl.textContent = "Enter email and password.";
        return;
      }

      if (submitButton) {
        submitButton.disabled = true;
        submitButton.textContent = "Signing In...";
      }
      if (errorEl) errorEl.textContent = "";

      try {
        const response = await fetch(endpoint, {
          method: "POST",
          credentials: "include",
          headers: {
            "Content-Type": "application/json",
            Accept: "application/json"
          },
          body: JSON.stringify({ email: email, password: password })
        });

        const payload = await response.json().catch(function () { return {}; });
        if (!response.ok || payload.error || payload.success === false) {
          throw new Error(payload.error || "Login failed.");
        }

        window.location.href = redirectPath;
      } catch (error) {
        if (errorEl) errorEl.textContent = error.message || "Login failed.";
      } finally {
        if (submitButton) {
          submitButton.disabled = false;
          submitButton.textContent = redirectPath.indexOf("/admin") === 0 ? "Sign In to Admin" : "Sign In to Staff";
        }
      }
    });
  }

  bindAuthForm("admin-login-form", "/api/admin/login", "/admin/dashboard");
  bindAuthForm("staff-login-form", "/api/staff/login", "/dashboard");
})();
