function copyText(btn) {
    const text = btn.parentElement.querySelector("pre").innerText;
    navigator.clipboard.writeText(text);

    btn.innerText = "Copied";
    setTimeout(() => btn.innerText = "Copy", 1500);
}


const clearBtn = document.getElementById("clearBtn");
if (clearBtn) {
    clearBtn.addEventListener("click", async () => {
        if (!confirm("Clear all requests?")) return;

        await fetch("/debug/clear", { method: "POST" });
        location.reload();
    });
}