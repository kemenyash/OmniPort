window.omniPortCulture = {
    setCulture: function (culture) {
        var normalized = culture === "en" ? "en" : "uk";
        var maxAge = 60 * 60 * 24 * 365;

        document.cookie = "omniport.lang=" + normalized +
            "; path=/; max-age=" + maxAge + "; samesite=lax";

        document.documentElement.lang = normalized;
    }
};
