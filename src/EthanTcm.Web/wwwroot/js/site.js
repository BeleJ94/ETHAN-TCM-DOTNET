(() => {
  const defaultPageSize = 10;
  const tableInstances = new WeakMap();
  let pendingForm = null;
  let confirmationModal = null;
  let actionFeedbackTimer = null;

  const normalize = (value) => value
    .replace(/\s+/g, " ")
    .trim()
    .toLocaleLowerCase();

  const toComparable = (value) => {
    const text = value.trim();
    const normalizedNumber = text.replace(/,/g, "");
    const numeric = Number(normalizedNumber);
    if (text !== "" && Number.isFinite(numeric)) {
      return { type: "number", value: numeric };
    }

    const date = Date.parse(text);
    if (!Number.isNaN(date) && /^\d{4}-\d{2}-\d{2}/.test(text)) {
      return { type: "date", value: date };
    }

    return { type: "text", value: normalize(text) };
  };

  const compareCells = (left, right, direction) => {
    const a = toComparable(left);
    const b = toComparable(right);
    const multiplier = direction === "asc" ? 1 : -1;

    if (a.type === b.type && a.value < b.value) {
      return -1 * multiplier;
    }

    if (a.type === b.type && a.value > b.value) {
      return 1 * multiplier;
    }

    return String(a.value).localeCompare(String(b.value)) * multiplier;
  };

  const createButton = (label, className, type = "button") => {
    const button = document.createElement("button");
    button.type = type;
    button.className = className;
    button.textContent = label;
    return button;
  };

  const shouldDisableSort = (heading, index, totalColumns) => {
    const text = normalize(heading.textContent || "");
    return heading.dataset.sort === "false" || text === "" || index === totalColumns - 1 && text === "";
  };

  const enhanceTable = (table) => {
    if (tableInstances.has(table)) {
      return;
    }

    const tbody = table.tBodies[0];
    const thead = table.tHead;
    if (!tbody || !thead || tbody.rows.length === 0) {
      return;
    }

    const rows = Array.from(tbody.rows);
    const headings = Array.from(thead.querySelectorAll("th"));
    const configuredPageSize = Number(table.dataset.pageSize || defaultPageSize);
    const initialPageSize = Number.isFinite(configuredPageSize) && configuredPageSize > 0
      ? configuredPageSize
      : defaultPageSize;
    const state = {
      page: 1,
      pageSize: initialPageSize,
      search: "",
      sortIndex: -1,
      sortDirection: "asc",
      rows
    };

    const wrapper = document.createElement("div");
    wrapper.className = "data-table";
    table.parentNode.insertBefore(wrapper, table);
    wrapper.appendChild(table);

    const toolbar = document.createElement("div");
    toolbar.className = "data-table-toolbar";

    const searchGroup = document.createElement("div");
    searchGroup.className = "data-table-search";
    const searchLabel = document.createElement("label");
    searchLabel.className = "data-table-label";
    searchLabel.textContent = "Dynamic search";
    const searchInput = document.createElement("input");
    searchInput.type = "search";
    searchInput.className = "form-control data-table-input";
    searchInput.placeholder = "Search this table";
    searchInput.autocomplete = "off";
    searchInput.setAttribute("aria-label", "Dynamic table search");
    searchLabel.appendChild(searchInput);
    searchGroup.appendChild(searchLabel);

    const actions = document.createElement("div");
    actions.className = "data-table-actions";
    const sizeLabel = document.createElement("label");
    sizeLabel.className = "data-table-size";
    sizeLabel.textContent = "Rows";
    const sizeSelect = document.createElement("select");
    sizeSelect.className = "form-select";
    [10, 25, 50, 100].forEach((size) => {
      const option = document.createElement("option");
      option.value = String(size);
      option.textContent = String(size);
      option.selected = size === state.pageSize;
      sizeSelect.appendChild(option);
    });
    sizeLabel.appendChild(sizeSelect);
    const resetButton = createButton("Reset", "btn btn-outline-secondary btn-sm");
    actions.append(sizeLabel, resetButton);
    toolbar.append(searchGroup, actions);
    wrapper.insertBefore(toolbar, table);

    const footer = document.createElement("div");
    footer.className = "data-table-footer";
    const summary = document.createElement("div");
    summary.className = "data-table-summary";
    const pagination = document.createElement("div");
    pagination.className = "data-table-pagination";
    footer.append(summary, pagination);
    wrapper.appendChild(footer);

    const emptyRow = document.createElement("tr");
    emptyRow.className = "data-table-empty-row";
    const emptyCell = document.createElement("td");
    emptyCell.colSpan = Math.max(headings.length, 1);
    emptyCell.className = "text-muted";
    emptyCell.textContent = "No matching record.";
    emptyRow.appendChild(emptyCell);

    const getFilteredRows = () => {
      const search = normalize(state.search);
      let filteredRows = state.rows.filter((row) => {
        if (!search) {
          return true;
        }

        return normalize(row.textContent || "").includes(search);
      });

      if (state.sortIndex >= 0) {
        filteredRows = filteredRows.slice().sort((left, right) =>
          compareCells(
            left.cells[state.sortIndex]?.textContent || "",
            right.cells[state.sortIndex]?.textContent || "",
            state.sortDirection));
      }

      return filteredRows;
    };

    const updateSortHeadings = () => {
      headings.forEach((heading, index) => {
        heading.classList.toggle("is-sorted", index === state.sortIndex);
        heading.classList.toggle("is-sorted-desc", index === state.sortIndex && state.sortDirection === "desc");
        const button = heading.querySelector(".data-table-sort-button");
        if (button) {
          button.setAttribute("aria-sort", index === state.sortIndex ? state.sortDirection : "none");
        }
      });
    };

    const render = () => {
      const filteredRows = getFilteredRows();
      const totalPages = Math.max(Math.ceil(filteredRows.length / state.pageSize), 1);
      state.page = Math.min(Math.max(state.page, 1), totalPages);
      const start = (state.page - 1) * state.pageSize;
      const visibleRows = filteredRows.slice(start, start + state.pageSize);

      tbody.replaceChildren();
      if (visibleRows.length === 0) {
        tbody.appendChild(emptyRow);
      } else {
        visibleRows.forEach((row) => tbody.appendChild(row));
      }

      const firstItem = filteredRows.length === 0 ? 0 : start + 1;
      const lastItem = Math.min(start + state.pageSize, filteredRows.length);
      summary.textContent = `${firstItem}-${lastItem} of ${filteredRows.length} records`;

      pagination.replaceChildren();
      const previous = createButton("Previous", "btn btn-outline-secondary btn-sm");
      previous.disabled = state.page === 1;
      previous.addEventListener("click", () => {
        state.page -= 1;
        render();
      });

      const pageStatus = document.createElement("span");
      pageStatus.className = "data-table-page-status";
      pageStatus.textContent = `Page ${state.page} / ${totalPages}`;

      const next = createButton("Next", "btn btn-outline-secondary btn-sm");
      next.disabled = state.page === totalPages;
      next.addEventListener("click", () => {
        state.page += 1;
        render();
      });

      pagination.append(previous, pageStatus, next);
      updateSortHeadings();
    };

    headings.forEach((heading, index) => {
      if (shouldDisableSort(heading, index, headings.length)) {
        return;
      }

      const label = heading.textContent.trim();
      const sortButton = createButton(label, "data-table-sort-button");
      sortButton.setAttribute("aria-label", `Sort by ${label}`);
      heading.textContent = "";
      heading.appendChild(sortButton);
      sortButton.addEventListener("click", () => {
        if (state.sortIndex === index) {
          state.sortDirection = state.sortDirection === "asc" ? "desc" : "asc";
        } else {
          state.sortIndex = index;
          state.sortDirection = "asc";
        }

        state.page = 1;
        render();
      });
    });

    searchInput.addEventListener("input", (event) => {
      state.search = event.target.value;
      state.page = 1;
      render();
    });

    sizeSelect.addEventListener("change", (event) => {
      state.pageSize = Number(event.target.value);
      state.page = 1;
      render();
    });

    resetButton.addEventListener("click", () => {
      state.search = "";
      state.page = 1;
      state.sortIndex = -1;
      state.sortDirection = "asc";
      state.pageSize = initialPageSize;
      searchInput.value = "";
      sizeSelect.value = String(initialPageSize);
      render();
    });

    tableInstances.set(table, state);
    render();
  };

  const enhanceResponsiveTable = (table) => {
    if (table.classList.contains("no-mobile-cards") || table.dataset.responsiveReady === "true") {
      return;
    }

    const headings = Array.from(table.querySelectorAll("thead th"))
      .map((heading) => heading.textContent.replace(/\s+/g, " ").trim());
    if (headings.length === 0) {
      return;
    }

    table.classList.add("mobile-card-table");
    table.querySelectorAll("tbody tr").forEach((row) => {
      const cells = Array.from(row.cells);
      if (cells.length === 1 && cells[0].colSpan > 1) {
        row.classList.add("mobile-card-empty-row");
        return;
      }

      cells.forEach((cell, index) => {
        const label = headings[index] || "";
        cell.dataset.label = label;
        if (!label) cell.classList.add("mobile-card-action");
      });
    });
    table.dataset.responsiveReady = "true";
  };

  const initialize = (root = document) => {
    root.querySelectorAll(".table-responsive table").forEach(enhanceResponsiveTable);
    root.querySelectorAll(".js-data-table").forEach(enhanceTable);
  };

  // Partials loaded asynchronously (for example dashboard details) need the
  // same responsive/table enhancements as the initial document.
  window.ethanTcmInitialize = initialize;

  const getFieldLabel = (field) => {
    if (field.id) {
      const explicitLabel = document.querySelector(`label[for="${CSS.escape(field.id)}"]`);
      if (explicitLabel) {
        return explicitLabel.textContent.trim();
      }
    }

    const nearbyLabel = field.closest("div")?.querySelector("label");
    if (nearbyLabel) {
      return nearbyLabel.textContent.trim();
    }

    return field.name || "Field";
  };

  const getFieldValue = (field) => {
    if (field.type === "file") {
      return field.files.length > 0
        ? Array.from(field.files).map((file) => file.name).join(", ")
        : "No file selected";
    }

    if (field.tagName === "SELECT") {
      return Array.from(field.selectedOptions).map((option) => option.textContent.trim()).join(", ");
    }

    if (field.type === "checkbox" || field.type === "radio") {
      return field.checked ? "Yes" : "No";
    }

    return field.value?.trim() || "-";
  };

  const buildConfirmationItems = (form) => {
    const items = [];
    const correspondenceTitle = document.querySelector("main h1")?.textContent.trim();
    const correspondenceSubject = document.querySelector("main h1")?.closest("div")?.querySelector("p")?.textContent.trim();
    if (correspondenceTitle && form.action.includes("Correspondence")) {
      items.push({ label: "Correspondence", value: correspondenceTitle });
    }
    if (correspondenceSubject && form.action.includes("Correspondence")) {
      items.push({ label: "Subject", value: correspondenceSubject });
    }
    const declarationTitle = document.querySelector(".declaration-heading h1")?.textContent.trim();
    const declarationPeriod = document.querySelector(".declaration-heading p")?.textContent.trim();
    if (declarationTitle) {
      items.push({ label: "Declaration", value: declarationTitle });
    }

    if (declarationPeriod) {
      items.push({ label: "Period / Status", value: declarationPeriod });
    }

    const actionButton = form.querySelector("button[type='submit']");
    if (actionButton) {
      items.push({ label: "Action", value: actionButton.textContent.trim() });
    }

    Array.from(form.elements).forEach((field) => {
      if (!field.name ||
          field.type === "hidden" ||
          field.type === "submit" ||
          field.type === "button" ||
          field.dataset.confirmIgnore === "true" ||
          field.name === "__RequestVerificationToken") {
        return;
      }

      items.push({
        label: getFieldLabel(field),
        value: getFieldValue(field)
      });
    });

    return items;
  };

  const ensureConfirmationModal = () => {
    let modal = document.getElementById("workflow-confirmation-modal");
    if (!modal) {
      modal = document.createElement("div");
      modal.id = "workflow-confirmation-modal";
      modal.className = "modal fade";
      modal.tabIndex = -1;
      modal.setAttribute("aria-hidden", "true");
      modal.innerHTML = `
        <div class="modal-dialog modal-dialog-centered">
          <div class="modal-content">
            <div class="modal-header">
              <h2 class="modal-title fs-5">Confirm action</h2>
              <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
              <p class="text-muted mb-3">Review the information before submitting.</p>
              <dl class="detail-list confirmation-summary"></dl>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancel</button>
              <button type="button" class="btn btn-primary js-confirm-submit">Confirm and submit</button>
            </div>
          </div>
        </div>`;
      document.body.appendChild(modal);
    }

    if (!confirmationModal && window.bootstrap?.Modal) {
      confirmationModal = new window.bootstrap.Modal(modal);
    }

    return modal;
  };

  const renderConfirmation = (form) => {
    const modal = ensureConfirmationModal();
    modal.querySelector(".modal-title").textContent = form.dataset.confirmTitle || "Confirm action";
    modal.querySelector(".modal-body > p").textContent = form.dataset.confirmMessage || "Review the information before submitting.";
    const confirmButton = modal.querySelector(".js-confirm-submit");
    const submitLabel = form.querySelector("button[type='submit'], button:not([type])")?.textContent.trim().toLowerCase() || "";
    const isDanger = form.dataset.confirmLevel === "danger" || /(reject|close|cancel|delete)/.test(submitLabel);
    confirmButton.className = `btn js-confirm-submit ${isDanger ? "btn-danger" : "btn-primary"}`;
    const summary = modal.querySelector(".confirmation-summary");
    summary.replaceChildren();

    buildConfirmationItems(form).forEach((item) => {
      const term = document.createElement("dt");
      term.textContent = item.label;
      const description = document.createElement("dd");
      description.textContent = item.value;
      summary.append(term, description);
    });

    return modal;
  };

  const ensureActionFeedback = () => {
    let feedback = document.getElementById("action-feedback");
    if (feedback) {
      return feedback;
    }

    feedback = document.createElement("div");
    feedback.id = "action-feedback";
    feedback.className = "action-feedback";
    feedback.setAttribute("aria-live", "polite");
    feedback.innerHTML = `
      <div class="action-loader" role="status" aria-hidden="true">
        <span class="action-loader-spinner"></span>
        <strong>Processing action</strong>
        <small>Please wait while ETHAN TCM updates the declaration.</small>
      </div>
      <div class="action-alert" role="dialog" aria-modal="true" aria-hidden="true">
        <span class="action-alert-icon"></span>
        <h2></h2>
        <p></p>
        <button type="button" class="btn btn-primary js-action-alert-close">OK</button>
      </div>`;
    document.body.appendChild(feedback);
    return feedback;
  };

  const showActionLoader = () => {
    const feedback = ensureActionFeedback();
    feedback.classList.remove("show-alert", "is-success", "is-error");
    feedback.classList.add("show-loader");
    feedback.querySelector(".action-alert")?.setAttribute("aria-hidden", "true");
    feedback.querySelector(".action-loader")?.removeAttribute("aria-hidden");
  };

  const hideActionFeedback = () => {
    const feedback = document.getElementById("action-feedback");
    if (!feedback) {
      return;
    }

    feedback.classList.remove("show-loader", "show-alert", "is-success", "is-error");
    feedback.querySelector(".action-alert")?.setAttribute("aria-hidden", "true");
    feedback.querySelector(".action-loader")?.setAttribute("aria-hidden", "true");
  };

  const showActionAlert = (message, success) => {
    const feedback = ensureActionFeedback();
    const alert = feedback.querySelector(".action-alert");
    const title = alert.querySelector("h2");
    const text = alert.querySelector("p");
    const icon = alert.querySelector(".action-alert-icon");

    if (actionFeedbackTimer) {
      window.clearTimeout(actionFeedbackTimer);
      actionFeedbackTimer = null;
    }

    title.textContent = success ? "Action completed" : "Action failed";
    text.textContent = message || (success ? "The action was completed successfully." : "The action could not be completed.");
    icon.textContent = success ? "OK" : "!";
    alert.removeAttribute("aria-hidden");
    feedback.querySelector(".action-loader")?.setAttribute("aria-hidden", "true");
    feedback.classList.remove("show-loader", "is-success", "is-error");
    feedback.classList.add("show-alert", success ? "is-success" : "is-error");

    actionFeedbackTimer = window.setTimeout(hideActionFeedback, success ? 2600 : 5200);
  };

  const refreshDeclarationDetails = async (url) => {
    const response = await fetch(url, {
      headers: { "X-Requested-With": "XMLHttpRequest" }
    });
    const html = await response.text();
    const parsed = new DOMParser().parseFromString(html, "text/html");
    const nextDetails = parsed.querySelector(".declaration-details");
    const currentDetails = document.querySelector(".declaration-details");

    if (!response.ok || !nextDetails || !currentDetails) {
      throw new Error("The declaration details could not be refreshed.");
    }

    currentDetails.replaceWith(nextDetails);
    initialize(nextDetails);
  };

  const submitAjaxForm = async (form) => {
    const submitButtons = form.querySelectorAll("button[type='submit']");
    submitButtons.forEach((button) => {
      button.disabled = true;
    });
    showActionLoader();

    try {
      const response = await fetch(form.action, {
        method: form.method || "POST",
        body: new FormData(form),
        headers: { "X-Requested-With": "XMLHttpRequest" }
      });

      const contentType = response.headers.get("content-type") || "";
      if (!contentType.includes("application/json")) {
        throw new Error("Unexpected server response.");
      }

      const payload = await response.json();
      if (payload.success && payload.redirectUrl) {
        showActionAlert(payload.message || "The action was completed successfully.", true);
        window.setTimeout(() => window.location.assign(payload.redirectUrl), 450);
      } else if (payload.success && payload.refreshUrl) {
        await refreshDeclarationDetails(payload.refreshUrl);
        showActionAlert(payload.message || "The action was completed successfully.", true);
      } else if (payload.success) {
        if (form.dataset.ajaxResetReason === "true") {
          const reason = form.querySelector("[name='Reason']");
          if (reason) reason.value = "";
        }
        showActionAlert(payload.message || "The action was completed successfully.", true);
      } else {
        showActionAlert(payload.message || "The action could not be completed.", false);
      }
    } catch (error) {
      showActionAlert(error.message || "The action could not be completed.", false);
    } finally {
      submitButtons.forEach((button) => {
        button.disabled = false;
      });
    }
  };

  document.addEventListener("submit", (event) => {
    const submittedForm = event.target.closest("form");
    const actionPath = submittedForm ? new URL(submittedForm.action, window.location.href).pathname : "";
    const isCorrespondencePost = submittedForm && (submittedForm.method || "get").toLowerCase() === "post" &&
      (actionPath.startsWith("/Correspondences/") || actionPath.startsWith("/CorrespondenceActions/") || actionPath.startsWith("/CorrespondenceOrganizations/"));
    const form = submittedForm?.classList.contains("js-confirm-ajax-form") || isCorrespondencePost ? submittedForm : null;
    if (!form) {
      return;
    }

    if (!form.checkValidity()) {
      return;
    }

    event.preventDefault();
    pendingForm = form;
    const modal = renderConfirmation(form);

    if (confirmationModal) {
      confirmationModal.show();
    } else if (window.confirm("Confirm this action?")) {
      submitAjaxForm(form);
    }
  });

  document.addEventListener("click", (event) => {
    const closeAlertButton = event.target.closest(".js-action-alert-close");
    if (closeAlertButton) {
      hideActionFeedback();
      return;
    }

    const button = event.target.closest(".js-confirm-submit");
    if (!button || !pendingForm) {
      return;
    }

    const form = pendingForm;
    pendingForm = null;
    button.disabled = true;
    button.textContent = "Submitting...";

    if (confirmationModal) {
      confirmationModal.hide();
    }

    submitAjaxForm(form).finally(() => {
      button.disabled = false;
      button.textContent = "Confirm and submit";
    });
  });

  document.addEventListener("DOMContentLoaded", () => {
    initialize();
  });
})();

(() => {
  const modalElement = document.getElementById("dashboard-kpi-modal");
  if (!modalElement || !window.bootstrap?.Modal) {
    return;
  }

  const modalBody = modalElement.querySelector(".js-kpi-modal-body");
  const modalTitle = modalElement.querySelector(".modal-title");
  const modal = new window.bootstrap.Modal(modalElement);
  let requestController = null;
  const isFrench = document.documentElement.lang.toLowerCase().startsWith("fr");

  const renderLoading = () => {
    modalBody.innerHTML = `
      <div class="kpi-modal-loading">
        <span class="spinner-border spinner-border-sm" aria-hidden="true"></span>
        <span>${isFrench ? "Chargement du détail…" : "Loading details…"}</span>
      </div>`;
  };

  const renderError = () => {
    modalBody.innerHTML = `
      <div class="kpi-modal-error" role="alert">
        <strong>${isFrench ? "Le détail n’a pas pu être chargé." : "The details could not be loaded."}</strong>
        <span>${isFrench ? "Fermez cette fenêtre puis réessayez." : "Close this window and try again."}</span>
      </div>`;
  };

  const loadDetails = async (url) => {
    requestController?.abort();
    requestController = new AbortController();
    renderLoading();

    try {
      const response = await fetch(url, {
        headers: { "X-Requested-With": "XMLHttpRequest" },
        signal: requestController.signal
      });
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      modalBody.innerHTML = await response.text();
      window.ethanTcmInitialize?.(modalBody);
      const detailTitle = modalBody.querySelector(".kpi-detail-header h2");
      modalTitle.textContent = detailTitle?.textContent?.trim() || (isFrench ? "Détail du KPI" : "KPI details");
    } catch (error) {
      if (error.name !== "AbortError") {
        renderError();
      }
    }
  };

  document.addEventListener("click", (event) => {
    const card = event.target.closest(".js-kpi-card");
    if (card && !event.ctrlKey && !event.metaKey && !event.shiftKey && event.button === 0) {
      event.preventDefault();
      modalTitle.textContent = isFrench ? "Détail du KPI" : "KPI details";
      modal.show();
      loadDetails(card.dataset.kpiUrl);
      return;
    }

    const pageLink = event.target.closest(".js-kpi-page");
    if (pageLink && !pageLink.classList.contains("disabled")) {
      event.preventDefault();
      loadDetails(pageLink.href);
    }
  });

  document.addEventListener("dashboard:open-details", (event) => {
    const url = event.detail?.url;
    if (!url) {
      return;
    }

    modalTitle.textContent = event.detail?.title || (isFrench ? "Détail du graphique" : "Chart details");
    modal.show();
    loadDetails(url);
  });

  modalElement.addEventListener("hidden.bs.modal", () => {
    requestController?.abort();
    requestController = null;
    renderLoading();
  });
})();

(() => {
  const modalElement = document.getElementById("documentPreviewModal");
  if (!modalElement || !window.bootstrap?.Modal) {
    return;
  }

  const modal = new window.bootstrap.Modal(modalElement);
  const modalTitle = modalElement.querySelector(".modal-title");
  const documentType = modalElement.querySelector(".document-preview-type");
  const frame = modalElement.querySelector(".document-preview-frame");
  const downloadLink = modalElement.querySelector(".document-preview-download");

  document.addEventListener("click", (event) => {
    const button = event.target.closest("[data-document-preview]");
    if (!button) {
      return;
    }

    event.preventDefault();
    const previewUrl = button.dataset.documentPreviewUrl;
    const downloadUrl = button.dataset.documentDownloadUrl || previewUrl;
    if (!previewUrl) {
      return;
    }

    modalTitle.textContent = button.dataset.documentTitle || "Document preview";
    documentType.textContent = button.dataset.documentType || "Document";
    downloadLink.href = downloadUrl;
    frame.src = previewUrl;
    modal.show();
  });

  modalElement.addEventListener("hidden.bs.modal", () => {
    frame.removeAttribute("src");
  });
})();
