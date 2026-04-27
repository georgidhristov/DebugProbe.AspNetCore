function copyText(btn) {
    const text = btn.parentElement.querySelector("pre").innerText;
    navigator.clipboard.writeText(text);

    btn.innerText = "Copied";
    setTimeout(() => btn.innerText = "Copy", 1500);
}