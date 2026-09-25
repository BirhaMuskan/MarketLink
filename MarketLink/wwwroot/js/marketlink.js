/* =====================================================
   MARKETLINK JAVASCRIPT
===================================================== */

document.addEventListener("DOMContentLoaded", function () {

    /* -----------------------------------------------
       Navbar shadow on scroll
    ------------------------------------------------ */

    const header = document.querySelector(".market-header");

    window.addEventListener("scroll", function () {

        if (window.scrollY > 30) {

            header.style.boxShadow =
                "0 8px 30px rgba(23, 59, 43, 0.08)";

        } else {

            header.style.boxShadow = "none";

        }

    });


    /* -----------------------------------------------
       Favorite buttons
    ------------------------------------------------ */

    const favoriteButtons =
        document.querySelectorAll(".favorite-btn");

    favoriteButtons.forEach(function (button) {

        button.addEventListener("click", function () {

            const icon = button.querySelector("i");

            if (icon.classList.contains("bi-heart")) {

                icon.classList.remove("bi-heart");

                icon.classList.add("bi-heart-fill");

                button.style.background = "#173b2b";
                button.style.color = "#ffffff";

            } else {

                icon.classList.remove("bi-heart-fill");

                icon.classList.add("bi-heart");

                button.style.background = "";
                button.style.color = "";

            }

        });

    });


    /* -----------------------------------------------
       Quick add buttons
    ------------------------------------------------ */

    const quickButtons =
        document.querySelectorAll(".quick-add");

    quickButtons.forEach(function (button) {

        button.addEventListener("click", function () {

            const original =
                button.innerHTML;

            button.innerHTML =
                '<i class="bi bi-check2"></i>';

            button.style.background = "#2e7d52";

            setTimeout(function () {

                button.innerHTML = original;

                button.style.background = "";

            }, 1500);

        });

    });


    /* -----------------------------------------------
       Smooth scroll
    ------------------------------------------------ */

    document.querySelectorAll('a[href^="#"]').forEach(function (link) {

        link.addEventListener("click", function (event) {

            const targetId =
                this.getAttribute("href");

            if (targetId === "#") {
                return;
            }

            const target =
                document.querySelector(targetId);

            if (target) {

                event.preventDefault();

                target.scrollIntoView({
                    behavior: "smooth",
                    block: "start"
                });

            }

        });

    });


    /* -----------------------------------------------
       Simple reveal animation
    ------------------------------------------------ */

    const revealElements =
        document.querySelectorAll(
            ".fresh-card, .step-card, .stat-item"
        );

    const observer =
        new IntersectionObserver(
            function (entries) {

                entries.forEach(function (entry) {

                    if (entry.isIntersecting) {

                        entry.target.style.opacity = "1";
                        entry.target.style.transform =
                            "translateY(0)";

                        observer.unobserve(
                            entry.target
                        );

                    }

                });

            },
            {
                threshold: 0.12
            }
        );


    revealElements.forEach(function (element) {

        element.style.opacity = "0";
        element.style.transform =
            "translateY(20px)";
        element.style.transition =
            "opacity .6s ease, transform .6s ease";

        observer.observe(element);

    });

});
/* =========================================================
   MARKETLINK - MARKET SEARCH AND FILTERS
========================================================= */

document.addEventListener("DOMContentLoaded", function () {

    const marketSearch = document.getElementById("marketSearch");

    const marketLocation = document.getElementById("marketLocation");

    const marketDay = document.getElementById("marketDay");

    const resetMarketFilters = document.getElementById("resetMarketFilters");

    const marketCards = document.querySelectorAll(".market-card-item");

    const marketResultCount = document.getElementById("marketResultCount");

    const noMarketResults = document.getElementById("noMarketResults");


    // Stop if Markets section is not present

    if (!marketSearch || !marketLocation || !marketDay) {

        return;

    }


    // FILTER FUNCTION

    function filterMarkets() {

        const searchValue = marketSearch.value
            .toLowerCase()
            .trim();

        const locationValue = marketLocation.value;

        const dayValue = marketDay.value;


        let visibleMarkets = 0;


        marketCards.forEach(function (card) {

            const marketName = card.dataset.name.toLowerCase();

            const marketLocationValue = card.dataset.location;

            const marketDays = card.dataset.days.toLowerCase();


            const matchesSearch =
                marketName.includes(searchValue);


            const matchesLocation =
                locationValue === "all" ||
                marketLocationValue === locationValue;


            const matchesDay =
                dayValue === "all" ||
                marketDays.includes(dayValue);


            if (
                matchesSearch &&
                matchesLocation &&
                matchesDay
            ) {

                card.classList.remove("d-none");

                visibleMarkets++;

            }
            else {

                card.classList.add("d-none");

            }

        });


        // UPDATE RESULT COUNT

        marketResultCount.textContent =
            visibleMarkets + " Markets Available";


        // SHOW / HIDE NO RESULTS

        if (visibleMarkets === 0) {

            noMarketResults.classList.remove("d-none");

        }
        else {

            noMarketResults.classList.add("d-none");

        }

    }


    // EVENTS

    marketSearch.addEventListener("input", filterMarkets);

    marketLocation.addEventListener("change", filterMarkets);

    marketDay.addEventListener("change", filterMarkets);


    // RESET FILTERS

    resetMarketFilters.addEventListener("click", function () {

        marketSearch.value = "";

        marketLocation.value = "all";

        marketDay.value = "all";

        filterMarkets();

    });


});