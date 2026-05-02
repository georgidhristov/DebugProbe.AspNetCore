window.runCompare = async function () {
    const id = window.location.pathname.split('/').pop();
    const base = document.getElementById('baseUrl').value.trim();
    const remoteId = document.getElementById('compareId').value.trim();

    if (!base || !remoteId) {
        alert('Fill both fields');
        return;
    }

    const url = base.replace(/\/$/, '') + '/debug/json/' + remoteId;

    document.getElementById('compareResult').innerHTML =
        '<b style="color:orange">Comparing...</b>';

    try {
        const res = await fetch('/debug/compare/' + id + '?url=' + encodeURIComponent(url));

        if (!res.ok) {
            const text = await res.json();
            document.getElementById('compareResult').innerHTML =
                `<b style="color:red">${text || 'Compare failed'}</b>`;
            return;
        }

        const result = await res.json();
        const data = result.diffs;

        let html = '';

        // -------- SECTIONS --------

        const environment = [
            { field: "Environment", local: result.environment?.local, remote: result.environment?.remote },
            { field: "Culture", local: result.culture?.local, remote: result.culture?.remote }
        ];

        const overview = [
            { field: "Method", local: result.method?.local, remote: result.method?.remote },
            { field: "Path", local: result.path?.local, remote: result.path?.remote },
            { field: "Status", local: result.status?.local, remote: result.status?.remote },
            { field: "Request Time", local: result.requestTime?.local, remote: result.requestTime?.remote },
        ];

        function renderSection(title, rows) {
            let html = `<h3>${title}</h3>`;
            html += '<table style="border-collapse:collapse;width:100%">';
            html += '<tr><th>Field</th><th>Local</th><th>Remote</th></tr>';

            rows.forEach(d => {
                const changed = d.local !== d.remote;

                html += `<tr style="${changed ? 'background:rgba(255,200,0,0.12)' : ''}">
                    <td>${d.field}</td>
                    <td style="${changed ? 'color:#e74c3c' : ''}">${d.local ?? ''}</td>
                    <td style="${changed ? 'color:#e74c3c' : ''}">${d.remote ?? ''}</td>
                </tr>`;
            });

            html += '</table>';
            return html;
        }

        html += renderSection("Environment", environment);
        html += renderSection("Overview", overview);

        // -------- REQUEST --------

        const requestChangedFields = data
            .filter(d => d.type === "request")
            .map(d => d.field);

        html += '<h3>Request</h3>';
        html += renderSideBySideJson(result.requestBody, requestChangedFields);

        // -------- RESPONSE --------

        const responseChangedFields = data
            .filter(d => d.type === "response")
            .map(d => d.field);

        html += '<h3>Response</h3>';
        html += renderSideBySideJson(result.responseBody, responseChangedFields);

        document.getElementById('compareResult').innerHTML = html;

    } catch {
        document.getElementById('compareResult').innerHTML =
            '<b style="color:red">Error during compare</b>';
    }
};

// -------- HELPERS --------

function renderSideBySideJson(data, changedFields) {
    return `
        <div style="display:flex; gap:20px;">
            <div style="flex:1">
                <b>Local</b>
                ${renderJsonWithHighlight(data?.local || '', changedFields)}
            </div>
            <div style="flex:1">
                <b>Remote</b>
                ${renderJsonWithHighlight(data?.remote || '', changedFields)}
            </div>
        </div>
    `;
}

function renderJsonWithHighlight(json, changedFields) {
    if (!json || json.trim() === "" || json === "{}") {
        return `
            <div class="code-block">
                <button class="copy-btn" onclick="copyText(this)">Copy</button>
                <pre>(empty)</pre>
            </div>
        `;
    }

    try {
        const obj = JSON.parse(json);
        const pretty = JSON.stringify(obj, null, 2);

        if (!pretty || pretty === "{}") {
            return `
                <div class="code-block">
                    <button class="copy-btn" onclick="copyText(this)">Copy</button>
                    <pre>(empty)</pre>
                </div>
            `;
        }

        const lines = pretty.split('\n');

        const content = lines.map(line => {
            const isChanged = changedFields.some(f => {
                const key = f.split('.').pop();
                return line.includes(`"${key}"`);
            });

            return `<div class="${isChanged ? 'diff-line' : ''}">${line}</div>`;
        }).join('');

        return `
            <div class="code-block">
                <button class="copy-btn" onclick="copyText(this)">Copy</button>
                <pre>${content}</pre>
            </div>
        `;
    } catch {
        return `
            <div class="code-block">
                <button class="copy-btn" onclick="copyText(this)">Copy</button>
                <pre>(empty)</pre>
            </div>
        `;
    }
}