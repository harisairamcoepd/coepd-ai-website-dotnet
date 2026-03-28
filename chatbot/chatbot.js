(function () {
  if (window.__COEPD_CHATBOT_LOADED__) return;
  window.__COEPD_CHATBOT_LOADED__ = true;

  const CHATBOT_FLOW_VERSION = "2026-03-28-simple-chat";

  const state = {
    isOpen: false,
    userId: localStorage.getItem("coepd_chat_uid") || `coepd_${Date.now()}`,
    loading: false,
    started: false,
    initialized: false,
  };
  localStorage.setItem("coepd_chat_uid", state.userId);

  function injectStyles() {
    const css = document.createElement("link");
    css.rel = "stylesheet";
    css.href = "/chatbot/chatbot.css?v=7";
    document.head.appendChild(css);
  }

  function injectMarkup() {
    const launcher = document.createElement("button");
    launcher.id = "coepd-chatbot-launcher";
    launcher.type = "button";
    launcher.setAttribute("aria-label", "Open chatbot");
    launcher.innerHTML = '<img src="/chatbot/robot.svg" alt="Robot"/>';

    const widget = document.createElement("section");
    widget.id = "coepd-chatbot-widget";
    widget.innerHTML = `
      <div class="cb-head">
        <div>
          <div class="cb-title">COEPD Support</div>
          <div class="cb-status">Lead Assistance</div>
        </div>
        <div class="cb-head-actions">
          <button type="button" id="cb-min">_</button>
          <button type="button" id="cb-reset">Reset</button>
          <button type="button" id="cb-close">Close</button>
        </div>
      </div>
      <div class="cb-progress"><div id="cb-progress-fill"></div></div>
      <div class="cb-log" id="cb-log"></div>
      <div class="cb-input-wrap">
        <div class="cb-options" id="cb-options"></div>
        <form id="cb-form" class="cb-input-row">
          <input id="cb-input" placeholder="Enter your message..." autocomplete="off" />
          <button type="submit">Send</button>
        </form>
      </div>
    `;

    document.body.appendChild(launcher);
    document.body.appendChild(widget);
  }

  function nowTime() {
    const d = new Date();
    return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  }

  function scrollLogToBottom() {
    const log = document.getElementById("cb-log");
    if (!log) return;
    requestAnimationFrame(() => {
      log.scrollTo({ top: log.scrollHeight, behavior: "smooth" });
    });
  }

  function appendMessage(role, html, typing) {
    const log = document.getElementById("cb-log");
    if (!log) return null;
    const normalizedHtml = typing ? "" : formatMessage(html);
    const lastRow = log.lastElementChild;
    if (!typing && lastRow && lastRow.classList.contains(role)) {
      const lastBubble = lastRow.querySelector(".cb-bubble");
      if (lastBubble && lastBubble.getAttribute("data-message-html") === normalizedHtml) {
        return lastRow;
      }
    }
    const row = document.createElement("div");
    row.className = `cb-row ${role}`;
    const bubble = document.createElement("div");
    bubble.className = "cb-bubble";

    if (typing) {
      bubble.innerHTML = '<div class="cb-typing"><span></span><span></span><span></span></div>';
    } else {
      bubble.innerHTML = normalizedHtml;
      bubble.setAttribute("data-message-html", normalizedHtml);
      const time = document.createElement("div");
      time.className = "cb-time";
      time.textContent = nowTime();
      bubble.appendChild(time);
    }

    row.appendChild(bubble);
    log.appendChild(row);
    scrollLogToBottom();
    return row;
  }

  function escapeHtml(value) {
    return String(value || "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  function formatMessage(value) {
    let html = escapeHtml(value);
    html = html.replace(/\*\*(.*?)\*\*/g, "<strong>$1</strong>");
    html = html.replace(/https:\/\/[^\s<]+/g, (url) => `<a href="${url}" target="_blank" rel="noopener noreferrer">${url}</a>`);
    html = html.replace(/\+91\s?88850\s?24387/g, '<a href="tel:+918885024387">+91 88850 24387</a>');
    html = html.replace(/\n/g, "<br>");
    return html;
  }

  function setProgress(value) {
    const bar = document.getElementById("cb-progress-fill");
    if (!bar) return;
    bar.style.width = `${Math.max(0, Math.min(100, value || 0))}%`;
  }

  function setOptions() {
    const wrap = document.getElementById("cb-options");
    if (wrap) wrap.innerHTML = "";
  }

  function setInputEnabled(enabled) {
    const input = document.getElementById("cb-input");
    const submitBtn = document.querySelector("#cb-form button[type='submit']");
    if (input) input.disabled = !enabled;
    if (submitBtn) submitBtn.disabled = !enabled;
    const optionButtons = document.querySelectorAll("#cb-options button");
    optionButtons.forEach((btn) => {
      btn.disabled = !enabled;
    });
  }

  async function submitLeadPayload(leadPayload) {
    if (!leadPayload || typeof leadPayload !== "object") return false;
    const payload = {
      name: String(leadPayload.name || "").trim(),
      phone: String(leadPayload.phone || "").trim(),
      email: String(leadPayload.email || "").trim().toLowerCase(),
      location: String(leadPayload.location || "").trim(),
      interested_domain: String(leadPayload.interested_domain || leadPayload.domain || "").trim(),
      whatsapp: String(leadPayload.whatsapp || "").trim(),
      source: "chatbot",
      created_at: new Date().toISOString(),
    };

    if (!payload.name || !payload.email || !payload.phone) return false;

    try {
      const res = await fetch((window.__API_BASE__ || "") + "/lead", {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify(payload),
      });
      if (!res.ok) return false;
      const contentType = String(res.headers.get("content-type") || "").toLowerCase();
      if (!contentType.includes("application/json")) return false;
      const data = await res.json().catch(() => ({}));
      return Boolean(data && data.ok === true && data.id);
    } catch (_err) {
      // Keep chat UX non-blocking even if lead sync fails.
      return false;
    }
  }

  async function sendMessage(message, showUserBubble = true) {
    if (state.loading || !message || !message.trim()) return;
    state.loading = true;
    setInputEnabled(false);

    const input = document.getElementById("cb-input");
    if (showUserBubble) {
      appendMessage("user", message);
    }
    input.value = "";

    const typingEl = appendMessage("bot", "", true);

    try {
      const minTypingDelay = 1000 + Math.floor(Math.random() * 1000);
      const responsePromise = fetch((window.__API_BASE__ || "") + "/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message: message.trim(), user_id: state.userId }),
      });
      const delayPromise = new Promise((resolve) => setTimeout(resolve, minTypingDelay));
      const [response] = await Promise.all([responsePromise, delayPromise]);

      if (!response.ok) {
        throw new Error(`Chat error ${response.status}`);
      }

      const data = await response.json();
      let botText = data.reply || data.text || "No response available.";
      if (typingEl) typingEl.remove();
      appendMessage("bot", botText);
      state.initialized = true;
      setOptions();
      setProgress(data.meta && typeof data.meta.progress === "number" ? data.meta.progress : 0);
      if (data.placeholder) input.placeholder = data.placeholder;
    } catch (_err) {
      if (typingEl) typingEl.remove();
      appendMessage("bot", "Something went wrong. Please try again.");
      setOptions();
    } finally {
      state.loading = false;
      setInputEnabled(true);
      input.focus();
    }
  }

  function bindEvents() {
    const launcher = document.getElementById("coepd-chatbot-launcher");
    const widget = document.getElementById("coepd-chatbot-widget");
    const form = document.getElementById("cb-form");
    const input = document.getElementById("cb-input");

    function ensureChatStarted() {
      if (state.started || state.initialized || document.getElementById("cb-log")?.children.length) return;
      state.started = true;
      const storedFlowVersion = localStorage.getItem("coepd_chat_flow_version");
      if (storedFlowVersion !== CHATBOT_FLOW_VERSION) {
        localStorage.setItem("coepd_chat_flow_version", CHATBOT_FLOW_VERSION);
        sendMessage("__restart__", false);
        return;
      }
      sendMessage("__init__", false);
    }

    launcher.addEventListener("click", function () {
      state.isOpen = !state.isOpen;
      widget.classList.toggle("open", state.isOpen);
      launcher.classList.toggle("open", state.isOpen);
      if (state.isOpen) {
        ensureChatStarted();
        input.focus();
      }
    });

    document.getElementById("cb-min").addEventListener("click", function () {
      state.isOpen = false;
      widget.classList.remove("open");
      launcher.classList.remove("open");
    });

    document.getElementById("cb-close").addEventListener("click", function () {
      state.isOpen = false;
      widget.classList.remove("open");
      launcher.classList.remove("open");
    });

    document.getElementById("cb-reset").addEventListener("click", function () {
      document.getElementById("cb-log").innerHTML = "";
      setProgress(0);
      setOptions();
      state.started = true;
      state.initialized = false;
      sendMessage("__restart__", false);
    });

    form.addEventListener("submit", function (event) {
      event.preventDefault();
      sendMessage(input.value, true);
    });
  }

  function init() {
    injectStyles();
    injectMarkup();
    bindEvents();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
