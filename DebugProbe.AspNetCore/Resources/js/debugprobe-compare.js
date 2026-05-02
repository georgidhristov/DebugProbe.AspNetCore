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
    const comparison = buildJsonLineComparison(data?.local || '', data?.remote || '', changedFields);

    return `
        <div class="json-compare">
            <div style="flex:1">
                <b>Local</b>
                ${renderAlignedJson(comparison.local, data?.local || '')}
            </div>
            <div style="flex:1">
                <b>Remote</b>
                ${renderAlignedJson(comparison.remote, data?.remote || '')}
            </div>
        </div>
    `;
}

function buildJsonLineComparison(localJson, remoteJson, changedFields) {
    const localValue = parseJson(localJson);
    const remoteValue = parseJson(remoteJson);

    if (localValue.ok || remoteValue.ok) {
        if (!localValue.ok && !remoteValue.ok) {
            return emptyJsonComparison();
        }

        return buildStructuredJsonComparison(
            localValue.value,
            remoteValue.value,
            localValue.ok,
            remoteValue.ok
        );
    }

    const localLines = getPrettyJsonLines(localJson);
    const remoteLines = getPrettyJsonLines(remoteJson);

    if (localLines.length === 0 && remoteLines.length === 0) {
        return emptyJsonComparison();
    }

    const aligned = alignLines(localLines, remoteLines);

    aligned.local.forEach((line, index) => {
        const remoteLine = aligned.remote[index];

        if (!line && remoteLine) {
            aligned.local[index] = { text: '', state: 'missing' };
            aligned.remote[index] = { text: remoteLine, state: 'added' };
            return;
        }

        if (line && !remoteLine) {
            aligned.local[index] = { text: line, state: 'added' };
            aligned.remote[index] = { text: '', state: 'missing' };
            return;
        }

        const changed = !areSameJsonLineForAlignment(line, remoteLine) || isKnownChangedLine(line, changedFields) || isKnownChangedLine(remoteLine, changedFields);
        aligned.local[index] = { text: line, state: changed ? 'changed' : '' };
        aligned.remote[index] = { text: remoteLine, state: changed ? 'changed' : '' };
    });

    return aligned;
}

function parseJson(json) {
    if (!json || json.trim() === "" || json === "{}") {
        return { ok: false, value: null };
    }

    try {
        return { ok: true, value: JSON.parse(json) };
    } catch {
        return { ok: false, value: null };
    }
}

function emptyJsonComparison() {
    return {
        local: [{ text: '(empty)', state: '' }],
        remote: [{ text: '(empty)', state: '' }]
    };
}

function buildStructuredJsonComparison(localValue, remoteValue, hasLocal, hasRemote) {
    const local = [];
    const remote = [];
    appendValueComparison(localValue, remoteValue, hasLocal, hasRemote, 0, false, local, remote);

    return { local, remote };
}

function appendValueComparison(localValue, remoteValue, hasLocal, hasRemote, depth, trailingComma, local, remote) {
    if (!hasLocal && hasRemote) {
        appendAddedOrMissingBlock(remoteValue, depth, trailingComma, 'remote', local, remote);
        return;
    }

    if (hasLocal && !hasRemote) {
        appendAddedOrMissingBlock(localValue, depth, trailingComma, 'local', local, remote);
        return;
    }

    if (isPlainObject(localValue) && isPlainObject(remoteValue)) {
        appendObjectComparison(localValue, remoteValue, depth, trailingComma, local, remote);
        return;
    }

    if (Array.isArray(localValue) && Array.isArray(remoteValue)) {
        appendArrayComparison(localValue, remoteValue, depth, trailingComma, local, remote);
        return;
    }

    const state = jsonEquals(localValue, remoteValue) ? '' : 'changed';
    local.push({ text: `${indent(depth)}${formatJsonPrimitive(localValue)}${trailingComma ? ',' : ''}`, state });
    remote.push({ text: `${indent(depth)}${formatJsonPrimitive(remoteValue)}${trailingComma ? ',' : ''}`, state });
}

function appendObjectComparison(localValue, remoteValue, depth, trailingComma, local, remote) {
    const keys = getObjectKeys(localValue, remoteValue);

    local.push({ text: `${indent(depth)}{`, state: '' });
    remote.push({ text: `${indent(depth)}{`, state: '' });

    keys.forEach((key, index) => {
        const isLast = index === keys.length - 1;
        const hasLocalKey = Object.prototype.hasOwnProperty.call(localValue, key);
        const hasRemoteKey = Object.prototype.hasOwnProperty.call(remoteValue, key);
        const keyState = hasLocalKey && hasRemoteKey && jsonEquals(localValue[key], remoteValue[key]) ? '' : getPresenceState(hasLocalKey, hasRemoteKey);

        if (isContainerValue(localValue[key]) || isContainerValue(remoteValue[key])) {
            local.push({
                text: hasLocalKey ? `${indent(depth + 1)}"${key}": ${getContainerStart(localValue[key])}` : '',
                state: hasLocalKey ? keyState : 'missing'
            });
            remote.push({
                text: hasRemoteKey ? `${indent(depth + 1)}"${key}": ${getContainerStart(remoteValue[key])}` : '',
                state: hasRemoteKey ? keyState : 'missing'
            });

            appendContainerChildren(localValue[key], remoteValue[key], hasLocalKey, hasRemoteKey, depth + 2, local, remote);

            local.push({
                text: hasLocalKey ? `${indent(depth + 1)}${getContainerEnd(localValue[key])}${isLast ? '' : ','}` : '',
                state: hasLocalKey ? '' : 'missing'
            });
            remote.push({
                text: hasRemoteKey ? `${indent(depth + 1)}${getContainerEnd(remoteValue[key])}${isLast ? '' : ','}` : '',
                state: hasRemoteKey ? '' : 'missing'
            });
            return;
        }

        const localText = hasLocalKey ? `${indent(depth + 1)}"${key}": ${formatJsonPrimitive(localValue[key])}${isLast ? '' : ','}` : '';
        const remoteText = hasRemoteKey ? `${indent(depth + 1)}"${key}": ${formatJsonPrimitive(remoteValue[key])}${isLast ? '' : ','}` : '';

        local.push({ text: localText, state: hasLocalKey ? keyState : 'missing' });
        remote.push({ text: remoteText, state: hasRemoteKey ? keyState : 'missing' });
    });

    local.push({ text: `${indent(depth)}}${trailingComma ? ',' : ''}`, state: '' });
    remote.push({ text: `${indent(depth)}}${trailingComma ? ',' : ''}`, state: '' });
}

function appendArrayComparison(localValue, remoteValue, depth, trailingComma, local, remote) {
    local.push({ text: `${indent(depth)}[`, state: '' });
    remote.push({ text: `${indent(depth)}[`, state: '' });
    appendArrayItems(localValue, remoteValue, depth + 1, local, remote);
    local.push({ text: `${indent(depth)}]${trailingComma ? ',' : ''}`, state: '' });
    remote.push({ text: `${indent(depth)}]${trailingComma ? ',' : ''}`, state: '' });
}

function appendContainerChildren(localValue, remoteValue, hasLocal, hasRemote, depth, local, remote) {
    if (Array.isArray(localValue) || Array.isArray(remoteValue)) {
        appendArrayItems(hasLocal ? localValue : [], hasRemote ? remoteValue : [], depth, local, remote);
        return;
    }

    const keys = getObjectKeys(hasLocal ? localValue : {}, hasRemote ? remoteValue : {});
    keys.forEach((key, index) => {
        const isLast = index === keys.length - 1;
        const hasLocalKey = hasLocal && Object.prototype.hasOwnProperty.call(localValue, key);
        const hasRemoteKey = hasRemote && Object.prototype.hasOwnProperty.call(remoteValue, key);

        appendNamedValueComparison(
            key,
            hasLocalKey ? localValue[key] : null,
            hasRemoteKey ? remoteValue[key] : null,
            hasLocalKey,
            hasRemoteKey,
            depth,
            !isLast,
            local,
            remote
        );
    });
}

function appendNamedValueComparison(key, localValue, remoteValue, hasLocal, hasRemote, depth, trailingComma, local, remote) {
    const keyState = hasLocal && hasRemote && jsonEquals(localValue, remoteValue) ? '' : getPresenceState(hasLocal, hasRemote);

    if (isContainerValue(localValue) || isContainerValue(remoteValue)) {
        local.push({
            text: hasLocal ? `${indent(depth)}"${key}": ${getContainerStart(localValue)}` : '',
            state: hasLocal ? keyState : 'missing'
        });
        remote.push({
            text: hasRemote ? `${indent(depth)}"${key}": ${getContainerStart(remoteValue)}` : '',
            state: hasRemote ? keyState : 'missing'
        });

        appendContainerChildren(localValue, remoteValue, hasLocal, hasRemote, depth + 1, local, remote);

        local.push({
            text: hasLocal ? `${indent(depth)}${getContainerEnd(localValue)}${trailingComma ? ',' : ''}` : '',
            state: hasLocal ? '' : 'missing'
        });
        remote.push({
            text: hasRemote ? `${indent(depth)}${getContainerEnd(remoteValue)}${trailingComma ? ',' : ''}` : '',
            state: hasRemote ? '' : 'missing'
        });
        return;
    }

    local.push({
        text: hasLocal ? `${indent(depth)}"${key}": ${formatJsonPrimitive(localValue)}${trailingComma ? ',' : ''}` : '',
        state: hasLocal ? keyState : 'missing'
    });
    remote.push({
        text: hasRemote ? `${indent(depth)}"${key}": ${formatJsonPrimitive(remoteValue)}${trailingComma ? ',' : ''}` : '',
        state: hasRemote ? keyState : 'missing'
    });
}

function appendArrayItems(localItems, remoteItems, depth, local, remote) {
    const pairs = alignArrayItems(localItems, remoteItems);

    pairs.forEach((pair, index) => {
        appendValueComparison(
            pair.localValue,
            pair.remoteValue,
            pair.hasLocal,
            pair.hasRemote,
            depth,
            index < pairs.length - 1,
            local,
            remote
        );
    });
}

function alignArrayItems(localItems, remoteItems) {
    const table = buildArrayLcsTable(localItems, remoteItems);
    const pairs = [];
    let i = 0;
    let j = 0;

    while (i < localItems.length || j < remoteItems.length) {
        if (i < localItems.length && j < remoteItems.length && jsonEquals(localItems[i], remoteItems[j])) {
            pairs.push({ localValue: localItems[i], remoteValue: remoteItems[j], hasLocal: true, hasRemote: true });
            i++;
            j++;
            continue;
        }

        const localStart = i;
        const remoteStart = j;

        while (i < localItems.length || j < remoteItems.length) {
            if (i < localItems.length && j < remoteItems.length && jsonEquals(localItems[i], remoteItems[j])) {
                break;
            }

            if (j >= remoteItems.length || (i < localItems.length && table[i + 1][j] >= table[i][j + 1])) {
                i++;
            } else {
                j++;
            }
        }

        appendUnmatchedArrayItems(
            localItems.slice(localStart, i),
            remoteItems.slice(remoteStart, j),
            pairs
        );
    }

    return pairs;
}

function appendUnmatchedArrayItems(localItems, remoteItems, pairs) {
    const sharedCount = Math.min(localItems.length, remoteItems.length);
    const extraRemoteCount = Math.max(0, remoteItems.length - localItems.length);
    const extraLocalCount = Math.max(0, localItems.length - remoteItems.length);
    const remoteOffset = extraRemoteCount > 0
        ? findBestArrayAlignmentOffset(localItems, remoteItems, extraRemoteCount)
        : 0;
    const localOffset = extraLocalCount > 0
        ? findBestArrayAlignmentOffset(remoteItems, localItems, extraLocalCount)
        : 0;

    for (let i = 0; i < remoteOffset; i++) {
        pairs.push({ localValue: null, remoteValue: remoteItems[i], hasLocal: false, hasRemote: true });
    }

    for (let i = 0; i < localOffset; i++) {
        pairs.push({ localValue: localItems[i], remoteValue: null, hasLocal: true, hasRemote: false });
    }

    for (let i = 0; i < sharedCount; i++) {
        pairs.push({
            localValue: localItems[localOffset + i],
            remoteValue: remoteItems[remoteOffset + i],
            hasLocal: true,
            hasRemote: true
        });
    }

    for (let i = remoteOffset + sharedCount; i < remoteItems.length; i++) {
        pairs.push({ localValue: null, remoteValue: remoteItems[i], hasLocal: false, hasRemote: true });
    }

    for (let i = localOffset + sharedCount; i < localItems.length; i++) {
        pairs.push({ localValue: localItems[i], remoteValue: null, hasLocal: true, hasRemote: false });
    }
}

function findBestArrayAlignmentOffset(shorterItems, longerItems, extraCount) {
    let bestOffset = 0;
    let bestScore = Number.NEGATIVE_INFINITY;

    for (let offset = 0; offset <= extraCount; offset++) {
        let score = 0;

        for (let i = 0; i < shorterItems.length; i++) {
            score += getArrayItemSimilarity(shorterItems[i], longerItems[offset + i]);
        }

        if (score > bestScore) {
            bestScore = score;
            bestOffset = offset;
        }
    }

    return bestOffset;
}

function getArrayItemSimilarity(localValue, remoteValue) {
    if (jsonEquals(localValue, remoteValue)) {
        return 1000;
    }

    if (Array.isArray(localValue) && Array.isArray(remoteValue)) {
        const length = Math.min(localValue.length, remoteValue.length);
        let score = 10 - Math.abs(localValue.length - remoteValue.length);

        for (let i = 0; i < length; i++) {
            score += getArrayItemSimilarity(localValue[i], remoteValue[i]);
        }

        return score;
    }

    if (isPlainObject(localValue) && isPlainObject(remoteValue)) {
        const keys = getObjectKeys(localValue, remoteValue);
        let score = 20;

        keys.forEach(key => {
            const hasLocal = Object.prototype.hasOwnProperty.call(localValue, key);
            const hasRemote = Object.prototype.hasOwnProperty.call(remoteValue, key);

            if (hasLocal && hasRemote) {
                score += 5;
                score += jsonEquals(localValue[key], remoteValue[key]) ? 20 : getArrayItemSimilarity(localValue[key], remoteValue[key]);
            } else {
                score -= 5;
            }
        });

        return score;
    }

    return typeof localValue === typeof remoteValue ? 1 : -10;
}

function buildArrayLcsTable(localItems, remoteItems) {
    const table = Array.from({ length: localItems.length + 1 }, () => Array(remoteItems.length + 1).fill(0));

    for (let i = localItems.length - 1; i >= 0; i--) {
        for (let j = remoteItems.length - 1; j >= 0; j--) {
            table[i][j] = jsonEquals(localItems[i], remoteItems[j])
                ? table[i + 1][j + 1] + 1
                : Math.max(table[i + 1][j], table[i][j + 1]);
        }
    }

    return table;
}

function appendAddedOrMissingBlock(value, depth, trailingComma, side, local, remote) {
    const lines = JSON.stringify(value, null, 2).split('\n');

    lines.forEach((line, index) => {
        const text = `${indent(depth)}${line}${index === lines.length - 1 && trailingComma ? ',' : ''}`;
        const isLocal = side === 'local';

        local.push({ text: isLocal ? text : '', state: isLocal ? 'added' : 'missing' });
        remote.push({ text: isLocal ? '' : text, state: isLocal ? 'missing' : 'added' });
    });
}

function getObjectKeys(localValue, remoteValue) {
    const keys = [];

    Object.keys(localValue || {}).forEach(key => keys.push(key));
    Object.keys(remoteValue || {}).forEach(key => {
        if (!keys.includes(key)) {
            keys.push(key);
        }
    });

    return keys;
}

function isContainerValue(value) {
    return Array.isArray(value) || isPlainObject(value);
}

function isPlainObject(value) {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function getContainerStart(value) {
    return Array.isArray(value) ? '[' : '{';
}

function getContainerEnd(value) {
    return Array.isArray(value) ? ']' : '}';
}

function getPresenceState(hasLocal, hasRemote) {
    if (hasLocal && hasRemote) {
        return 'changed';
    }

    return hasLocal ? 'added' : 'added';
}

function formatJsonPrimitive(value) {
    return JSON.stringify(value);
}

function jsonEquals(localValue, remoteValue) {
    return JSON.stringify(localValue) === JSON.stringify(remoteValue);
}

function indent(depth) {
    return '  '.repeat(depth);
}

function getPrettyJsonLines(json) {
    if (!json || json.trim() === "" || json === "{}") {
        return [];
    }

    try {
        const obj = JSON.parse(json);
        const pretty = JSON.stringify(obj, null, 2);

        if (!pretty || pretty === "{}") {
            return [];
        }

        return pretty.split('\n');
    } catch {
        return [];
    }
}

function alignLines(localLines, remoteLines) {
    const table = buildLcsTable(localLines, remoteLines);
    const local = [];
    const remote = [];
    let localIndex = 0;
    let remoteIndex = 0;

    while (localIndex < localLines.length || remoteIndex < remoteLines.length) {
        if (localIndex < localLines.length && remoteIndex < remoteLines.length && areSameJsonLineForAlignment(localLines[localIndex], remoteLines[remoteIndex])) {
            local.push(localLines[localIndex]);
            remote.push(remoteLines[remoteIndex]);
            localIndex++;
            remoteIndex++;
            continue;
        }

        const localStart = localIndex;
        const remoteStart = remoteIndex;

        while (localIndex < localLines.length || remoteIndex < remoteLines.length) {
            if (localIndex < localLines.length && remoteIndex < remoteLines.length && areSameJsonLineForAlignment(localLines[localIndex], remoteLines[remoteIndex])) {
                break;
            }

            if (remoteIndex >= remoteLines.length || (localIndex < localLines.length && table[localIndex + 1][remoteIndex] >= table[localIndex][remoteIndex + 1])) {
                localIndex++;
            } else {
                remoteIndex++;
            }
        }

        appendChangedBlock(
            localLines.slice(localStart, localIndex),
            remoteLines.slice(remoteStart, remoteIndex),
            local,
            remote
        );
    }

    return { local, remote };
}

function buildLcsTable(localLines, remoteLines) {
    const table = Array.from({ length: localLines.length + 1 }, () => Array(remoteLines.length + 1).fill(0));

    for (let i = localLines.length - 1; i >= 0; i--) {
        for (let j = remoteLines.length - 1; j >= 0; j--) {
            table[i][j] = areSameJsonLineForAlignment(localLines[i], remoteLines[j])
                ? table[i + 1][j + 1] + 1
                : Math.max(table[i + 1][j], table[i][j + 1]);
        }
    }

    return table;
}

function appendChangedBlock(localBlock, remoteBlock, local, remote) {
    const matchedRemoteIndexes = new Set();

    localBlock.forEach(localLine => {
        const remoteIndex = remoteBlock.findIndex((remoteLine, index) =>
            !matchedRemoteIndexes.has(index) && areComparableJsonLines(localLine, remoteLine));

        if (remoteIndex >= 0) {
            matchedRemoteIndexes.add(remoteIndex);
            local.push(localLine);
            remote.push(remoteBlock[remoteIndex]);
        } else {
            local.push(localLine);
            remote.push(null);
        }
    });

    remoteBlock.forEach((remoteLine, index) => {
        if (matchedRemoteIndexes.has(index)) {
            return;
        }

        local.push(null);
        remote.push(remoteLine);
    });
}

function areComparableJsonLines(localLine, remoteLine) {
    const localKey = getJsonLineKey(localLine);
    const remoteKey = getJsonLineKey(remoteLine);

    if (localKey || remoteKey) {
        return localKey === remoteKey;
    }

    return getJsonLineShape(localLine) === getJsonLineShape(remoteLine);
}

function areSameJsonLineForAlignment(localLine, remoteLine) {
    return normalizeJsonLineForAlignment(localLine) === normalizeJsonLineForAlignment(remoteLine);
}

function normalizeJsonLineForAlignment(line) {
    return (line || '').replace(/,\s*$/, '');
}

function getJsonLineKey(line) {
    const match = line?.match(/^\s*"([^"]+)"\s*:/);
    return match ? match[1] : '';
}

function getJsonLineShape(line) {
    return (line || '').trim().replace(/"([^"\\]|\\.)*"/g, '""').replace(/-?\d+(\.\d+)?/g, '0');
}

function isKnownChangedLine(line, changedFields) {
    return changedFields.some(f => {
        const key = f.split('.').pop().replace(/\[\d+\]/g, '');
        return key && line.includes(`"${key}"`);
    });
}

function renderAlignedJson(lines, originalJson) {
    const copyValue = getCopyValue(originalJson, lines);
    const content = lines.map(line => {
        const className = line.state ? `diff-line diff-line-${line.state}` : '';
        const text = line.text ? escapeHtml(line.text) : '&nbsp;';

        return `<div class="${className}">${text}</div>`;
    }).join('');

    return `
        <div class="code-block">
            <button class="copy-btn" onclick="copyText(this)">Copy</button>
            <pre data-copy="${escapeHtml(copyValue)}">${content}</pre>
        </div>
    `;
}

function getCopyValue(json, renderedLines) {
    const prettyLines = getPrettyJsonLines(json);

    if (prettyLines.length > 0) {
        return prettyLines.join('\n');
    }

    return renderedLines.map(line => line.text).join('\n');
}

function escapeHtml(value) {
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}
