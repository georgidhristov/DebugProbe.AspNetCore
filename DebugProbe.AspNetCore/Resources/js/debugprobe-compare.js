/**
 * DebugProbe Compare Script
 * -------------------------
 * Handles client-side trace comparison and renders results in the UI.
 */

async function runCompare() {
    const id = window.location.pathname.split('/').pop();
    const base = document.getElementById('baseUrl').value.trim();
    const remoteId = document.getElementById('compareId').value.trim();

    if (!base || !remoteId) {
        alert('Fill both fields');
        return;
    }

    const cleanBase = base.replace(/\/$/, '');
    const url = cleanBase + '/debug/json/' + remoteId;

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

        if (data.length === 0) {
            html = '<b style="color:green">No differences</b>';
        } else {

            const groups = {};

            groups["Basic"] = [
                {
                    field: "Status",
                    local: result.status.local,
                    remote: result.status.remote
                },
                {
                    field: "Environment",
                    local: result.environment.local,
                    remote: result.environment.remote
                },
                {
                    field: "Culture",
                    local: result.culture.local,
                    remote: result.culture.remote
                }
            ];

            data.forEach(d => {
                const match = d.field.match(/^\[(\d+)\]\.(.+)$/);

                if (!match) {
                    if (!groups["Root"]) groups["Root"] = [];

                    groups["Root"].push({
                        field: d.field,
                        local: d.local,
                        remote: d.remote
                    });

                    return;
                }

                const index = match[1];
                const field = match[2];

                if (!groups[index]) groups[index] = [];

                groups[index].push({
                    field,
                    local: d.local,
                    remote: d.remote
                });
            });

            html += '<table style="border-collapse:collapse;width:100%">';
            html += '<tr><th>Field</th><th>Local</th><th>Remote</th></tr>';

            ["Basic", ...Object.keys(groups).filter(k => k !== "Basic")]
                .forEach(index => {

                    html += `<tr style="background:#eee">
                                <td colspan="3">
                                    <b>${index === "Basic" ? "Basic" : "Item [" + index + "]"}</b>
                                </td>
                             </tr>`;

                    groups[index].forEach(d => {
                        const changed = d.local !== d.remote;

                        html += `<tr style="${changed ? 'background:#fff3cd' : ''}">
                            <td style="padding-left:20px">${d.field}</td>
                            <td style="${changed ? 'color:#e74c3c' : ''}">${d.local}</td>
                            <td style="${changed ? 'color:#e74c3c' : ''}">${d.remote}</td>
                        </tr>`;
                    });
                });

            html += '</table>';
        }

        document.getElementById('compareResult').innerHTML = html;

    } catch {
        document.getElementById('compareResult').innerHTML =
            '<b style="color:red">Error during compare</b>';
    }
}