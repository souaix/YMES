(() => {
    document.querySelectorAll("[data-menu-tab]").forEach(tab => {
        tab.addEventListener("click", () => {
            const name = tab.dataset.menuTab;
            document.querySelectorAll("[data-menu-tab]").forEach(item => {
                const active = item === tab;
                item.classList.toggle("active", active);
                item.setAttribute("aria-selected", active ? "true" : "false");
            });
            document.querySelectorAll("[data-menu-panel]").forEach(panel => panel.hidden = panel.dataset.menuPanel !== name);
        });
    });
    document.querySelectorAll("[data-menu-search]").forEach(search => {
        search.addEventListener("input", () => {
            const query = search.value.trim().toLocaleLowerCase();
            document.querySelectorAll("[data-menu-item]").forEach(item => item.hidden = query && !item.textContent.toLocaleLowerCase().includes(query));
        });
    });
    document.querySelectorAll("[data-focus-field]").forEach(button => button.addEventListener("click", () => {
        const target = document.getElementById(button.dataset.focusField);
        if (!target) return;
        target.focus();
        if (typeof target.showPicker === "function") { try { target.showPicker(); } catch { } }
    }));
    document.querySelectorAll("[data-close-window]").forEach(link => link.addEventListener("click", event => {
        event.preventDefault();
        if (history.length > 1) history.back(); else window.location.href = "/YMES/Equipment";
    }));
    document.querySelectorAll("[data-export-csv]").forEach(button => {
        button.addEventListener("click", () => {
            const table = document.querySelector(button.dataset.exportTable || ".list-panel .table-scroll table");
            if (!table) return;
            const csv = [...table.querySelectorAll("tr")].map(row => [...row.querySelectorAll("th,td")].map(cell => `"${cell.innerText.replace(/\s+/g, " ").trim().replaceAll('"', '""')}"`).join(",")).join("\r\n");
            const blob = new Blob(["\uFEFF", csv], { type: "text/csv;charset=utf-8" });
            const url = URL.createObjectURL(blob);
            const link = document.createElement("a");
            link.href = url;
            link.download = button.dataset.filename || "master-data.csv";
            link.click();
            URL.revokeObjectURL(url);
        });
    });
    document.querySelectorAll("[data-print]").forEach(button => button.addEventListener("click", () => window.print()));
    document.querySelectorAll(".product-definition").forEach(form => {
        const quantity = form.querySelector("[data-lot-qty]");
        const modes = form.querySelectorAll("[data-lot-qty-mode]");
        if (!quantity || !modes.length) return;
        const syncQuantity = () => { quantity.disabled = form.querySelector("[data-lot-qty-mode]:checked")?.value !== "standard"; };
        modes.forEach(mode => mode.addEventListener("change", syncQuantity));
        syncQuantity();
    });

    document.querySelectorAll("[data-process-graph]").forEach(form => {
        const nodeBody = form.querySelector("[data-node-rows]");
        const edgeBody = form.querySelector("[data-edge-rows]");
        const preview = form.querySelector("[data-flow-preview]");
        const nodeJson = form.querySelector("[data-nodes-json]");
        const edgeJson = form.querySelector("[data-edges-json]");
        const makeInput = (value, prop, type = "text") => {
            const input = document.createElement("input");
            input.className = "graph-row-input";
            input.type = type;
            input.value = value ?? "";
            input.dataset.prop = prop;
            return input;
        };
        let nodes = [];
        let edges = [];
        try { nodes = JSON.parse(nodeJson.value || "[]"); } catch { nodes = []; }
        try { edges = JSON.parse(edgeJson.value || "[]"); } catch { edges = []; }

        const readNodes = () => [...nodeBody.querySelectorAll("tr")].map(row => ({
            nodeId: row.dataset.nodeId,
            nodeNo: row.querySelector('[data-prop="nodeNo"]').value.trim(),
            nodeType: row.querySelector('[data-prop="nodeType"]').value,
            sequence: Number(row.querySelector('[data-prop="sequence"]').value || 0),
            operationNo: row.querySelector('[data-prop="operationNo"]').value.trim(),
            description: row.querySelector('[data-prop="description"]').value.trim()
        }));
        const readEdges = () => [...edgeBody.querySelectorAll("tr")].map(row => ({
            fromNodeId: row.querySelector('[data-prop="fromNodeId"]').value,
            toNodeId: row.querySelector('[data-prop="toNodeId"]').value,
            action: row.querySelector('[data-prop="action"]').value.trim()
        }));
        const sync = () => {
            const currentNodes = readNodes();
            const currentEdges = readEdges();
            nodeJson.value = JSON.stringify(currentNodes);
            edgeJson.value = JSON.stringify(currentEdges);
            preview.replaceChildren();
            currentNodes.sort((a, b) => a.sequence - b.sequence).forEach((node, index) => {
                if (index) {
                    const arrow = document.createElement("span"); arrow.className = "graph-flow-arrow"; arrow.textContent = "➜"; preview.append(arrow);
                }
                const chip = document.createElement("span");
                chip.className = `graph-flow-node ${node.nodeType.toLowerCase()}`;
                chip.textContent = `${node.nodeNo || node.nodeType}${node.operationNo ? ` · ${node.operationNo}` : ""}`;
                preview.append(chip);
            });
        };
        const refreshEdgeSelectors = () => {
            const currentNodes = readNodes();
            edgeBody.querySelectorAll("tr").forEach(row => {
                for (const prop of ["fromNodeId", "toNodeId"]) {
                    const select = row.querySelector(`[data-prop="${prop}"]`);
                    const previous = select.value;
                    select.replaceChildren();
                    currentNodes.forEach(node => {
                        const option = document.createElement("option"); option.value = node.nodeId; option.textContent = `${node.nodeNo} (${node.nodeType})`; select.append(option);
                    });
                    if (currentNodes.some(node => node.nodeId === previous)) select.value = previous;
                }
            });
        };
        const appendNode = node => {
            const row = document.createElement("tr"); row.dataset.nodeId = node.nodeId;
            const cell = () => { const td = document.createElement("td"); row.append(td); return td; };
            cell().append(makeInput(node.sequence, "sequence", "number"));
            cell().append(makeInput(node.nodeNo, "nodeNo"));
            const type = document.createElement("select"); type.className = "graph-row-input"; type.dataset.prop = "nodeType";
            [["START", "起點"], ["OP", "作業站"], ["END", "終點"]].forEach(([value, label]) => { const option = document.createElement("option"); option.value = value; option.textContent = label; type.append(option); });
            type.value = node.nodeType || "OP"; cell().append(type);
            cell().append(makeInput(node.operationNo, "operationNo"));
            cell().append(makeInput(node.description, "description"));
            const remove = document.createElement("button"); remove.type = "button"; remove.className = "graph-row-remove"; remove.textContent = "移除";
            const actionCell = cell(); actionCell.append(remove);
            remove.disabled = ["START", "END"].includes(type.value.toUpperCase());
            remove.addEventListener("click", () => { row.remove(); refreshEdgeSelectors(); sync(); });
            type.addEventListener("change", () => { remove.disabled = ["START", "END"].includes(type.value); sync(); });
            row.addEventListener("input", sync);
            nodeBody.append(row);
        };
        const appendEdge = edge => {
            const row = document.createElement("tr");
            const from = document.createElement("select"); from.className = "graph-row-input"; from.dataset.prop = "fromNodeId";
            const to = document.createElement("select"); to.className = "graph-row-input"; to.dataset.prop = "toNodeId";
            const nodesNow = readNodes();
            for (const select of [from, to]) nodesNow.forEach(node => { const option = document.createElement("option"); option.value = node.nodeId; option.textContent = `${node.nodeNo} (${node.nodeType})`; select.append(option); });
            from.value = edge.fromNodeId || nodesNow[0]?.nodeId || ""; to.value = edge.toNodeId || nodesNow.at(-1)?.nodeId || "";
            const td1 = document.createElement("td"); td1.append(from); row.append(td1);
            const td2 = document.createElement("td"); td2.append(to); row.append(td2);
            const td3 = document.createElement("td"); td3.append(makeInput(edge.action || "pass", "action")); row.append(td3);
            const remove = document.createElement("button"); remove.type = "button"; remove.className = "graph-row-remove"; remove.textContent = "移除";
            const td4 = document.createElement("td"); td4.append(remove); row.append(td4);
            row.addEventListener("input", sync); row.addEventListener("change", sync); remove.addEventListener("click", () => { row.remove(); sync(); });
            edgeBody.append(row);
        };
        nodes.sort((a, b) => a.sequence - b.sequence).forEach(appendNode);
        edges.forEach(appendEdge);
        refreshEdgeSelectors(); sync();
        form.querySelector("[data-add-node]").addEventListener("click", () => {
            const current = readNodes(); const seq = Math.max(0, ...current.map(node => node.sequence)) + 10;
            appendNode({ nodeId: `node-${Date.now()}-${Math.random().toString(16).slice(2, 6)}`, nodeNo: `OP${seq}`, nodeType: "OP", sequence: seq, operationNo: "", description: "" });
            refreshEdgeSelectors(); sync();
        });
        form.querySelector("[data-add-edge]").addEventListener("click", () => { const n = readNodes(); appendEdge({ fromNodeId: n[0]?.nodeId, toNodeId: n[1]?.nodeId, action: "pass" }); sync(); });
        form.addEventListener("submit", sync);
    });
})();

document.addEventListener("click", async event => {
    const target = event.target;
    const button = target instanceof Element ? target.closest("[data-ymes-logout]") : null;
    if (!button) return;

    button.disabled = true;
    const shell = button.closest(".app-shell");
    const apiBase = shell?.dataset.apiBase || "/YMES/API";
    try {
        await fetch(`${apiBase}/auth/logout`, { method: "POST", credentials: "include" });
    } finally {
        window.location.assign(shell?.dataset.loginUrl || "/YMES/");
    }
});
