// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Smooth scrolling for anchor links
$(document).ready(function() {
    // Add smooth scrolling to all anchor links
    $("a[href^='#'], a[href*='/#']").on('click', function(event) {
        // Make sure this.hash has a value before overriding default behavior
        if (this.hash !== "") {
            // Store hash
            var hash = this.hash;
            
            // Handle URLs with fragment identifiers (for navigation from other pages)
            if (this.href.includes("/#")) {
                // If we're not on the homepage and trying to navigate to a homepage section
                if (!window.location.pathname.match(/^\/$|^\/index(.html)?$/i)) {
                    // Let the default behavior take over (navigate to homepage with hash)
                    return;
                }
            }
            
            // Prevent default anchor click behavior
            event.preventDefault();
            
            // Adjust target element to account for header height
            var targetOffset = $(hash).offset().top - 80;
            
            // Using jQuery's animate() method to add smooth page scroll
            $('html, body').animate({
                scrollTop: targetOffset
            }, 800, function(){
                // Add hash (#) to URL when done scrolling (default click behavior)
                window.location.hash = hash;
            });
        }
    });
    
    // When page loads with a hash in URL, adjust scroll position for fixed header
    if (window.location.hash) {
        // Wait a moment for the page to fully load
        setTimeout(function() {
            var targetOffset = $(window.location.hash).offset().top - 80;
            $('html, body').scrollTop(targetOffset);
        }, 100);
    }
});
