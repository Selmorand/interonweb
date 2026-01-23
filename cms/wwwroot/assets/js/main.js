/**
 * AI Readiness Platform - Main JavaScript
 * Simple, no dependencies
 */

// Mobile navigation toggle
function toggleNav() {
  const nav = document.getElementById('nav');
  if (nav) {
    nav.classList.toggle('active');
  }
}

// Close nav when clicking outside
document.addEventListener('click', function(e) {
  const nav = document.getElementById('nav');
  const toggle = document.querySelector('.nav-toggle');
  if (nav && toggle && !nav.contains(e.target) && !toggle.contains(e.target)) {
    nav.classList.remove('active');
  }
});

// Smooth scroll for anchor links
document.addEventListener('DOMContentLoaded', function() {
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function(e) {
      e.preventDefault();
      const target = document.querySelector(this.getAttribute('href'));
      if (target) {
        target.scrollIntoView({ behavior: 'smooth' });
      }
    });
  });
});
