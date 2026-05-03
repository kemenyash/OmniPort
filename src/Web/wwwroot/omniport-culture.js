window.omniPortCulture = {
    setCulture: function (culture) {
        var normalized = culture === "en" ? "en" : "uk";
        var maxAge = 60 * 60 * 24 * 365;

        document.cookie = "omniport.lang=" + normalized +
            "; path=/; max-age=" + maxAge + "; samesite=lax";

        document.documentElement.lang = normalized;
    }
};

window.omniPortClipboard = {
    copyText: async function (text) {
        var input = document.createElement("textarea");
        input.value = text;
        input.setAttribute("readonly", "");
        input.style.position = "fixed";
        input.style.opacity = "0";
        input.style.left = "-9999px";
        input.style.top = "0";
        document.body.appendChild(input);
        input.focus();
        input.select();
        input.setSelectionRange(0, input.value.length);

        try {
            if (document.execCommand("copy") === true) {
                return true;
            }
        }
        finally {
            document.body.removeChild(input);
        }

        if (navigator.clipboard && window.isSecureContext) {
            try {
                await navigator.clipboard.writeText(text);
                return true;
            }
            catch {
            }
        }

        return false;
    }
};
