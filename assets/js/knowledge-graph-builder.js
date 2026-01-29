/**
 * Knowledge Graph Builder - Railway API Integration
 * Connects to: https://web-production-9ed67.up.railway.app
 */

const API_URL = 'https://web-production-9ed67.up.railway.app';

// State
let currentJobId = null;
let currentSiteId = null;
let pollInterval = null;

// Start a crawl
async function startCrawl() {
  const urlInput = document.getElementById('url-input').value.trim();
  const maxDepth = parseInt(document.getElementById('max-depth').value);
  const maxPages = parseInt(document.getElementById('max-pages').value);

  // Validate inputs
  if (!urlInput) {
    showError('Please enter a website URL.');
    return;
  }

  // Add protocol if missing
  let url = urlInput;
  if (!url.startsWith('http://') && !url.startsWith('https://')) {
    url = 'https://' + url;
  }

  // Validate URL format
  try {
    new URL(url);
  } catch {
    showError('Please enter a valid URL (e.g., https://example.com)');
    return;
  }

  // Validate ranges
  if (maxDepth < 0 || maxDepth > 10) {
    showError('Max depth must be between 0 and 10');
    return;
  }

  if (maxPages < 1 || maxPages > 1000) {
    showError('Max pages must be between 1 and 1000');
    return;
  }

  // Disable form
  disableForm();

  // Show progress
  showProgress();
  updateProgress(0, 0, maxPages, 'PENDING', 'Starting crawl...');

  try {
    const response = await fetch(`${API_URL}/api/crawl/start`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ url, maxDepth, maxPages }),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to start crawl');
    }

    const data = await response.json();
    currentJobId = data.jobId;
    currentSiteId = data.siteId;

    // Start polling status
    startPolling();

  } catch (error) {
    console.error('Start crawl error:', error);
    showError(error.message || 'Failed to start crawl. Please try again.');
    enableForm();
  }
}

// Poll crawl status
function startPolling() {
  // Poll every 2 seconds
  pollInterval = setInterval(checkStatus, 2000);
  checkStatus(); // Check immediately
}

function stopPolling() {
  if (pollInterval) {
    clearInterval(pollInterval);
    pollInterval = null;
  }
}

async function checkStatus() {
  if (!currentJobId) return;

  try {
    const response = await fetch(`${API_URL}/api/crawl/${currentJobId}/status`);

    if (!response.ok) {
      throw new Error('Failed to check status');
    }

    const data = await response.json();
    const { status, pagesProcessed, maxPages, errorMessage } = data;

    // Calculate progress
    const progress = maxPages > 0 ? (pagesProcessed / maxPages) * 100 : 0;

    // Update UI
    let message = '';
    switch (status) {
      case 'PENDING':
        message = 'Waiting to start...';
        break;
      case 'IN_PROGRESS':
        message = `Crawling website... ${pagesProcessed} of ${maxPages} pages processed`;
        break;
      case 'COMPLETED':
        message = `Crawl complete! Building knowledge graph...`;
        stopPolling();
        await buildKnowledgeGraph();
        return;
      case 'FAILED':
        message = errorMessage || 'Crawl failed';
        stopPolling();
        showError(message);
        enableForm();
        return;
      default:
        message = 'Processing...';
    }

    updateProgress(progress, pagesProcessed, maxPages, status, message);

  } catch (error) {
    console.error('Status check error:', error);
    stopPolling();
    showError('Lost connection to server. Please try again.');
    enableForm();
  }
}

// Build knowledge graph
async function buildKnowledgeGraph() {
  if (!currentSiteId) return;

  updateProgress(100, 0, 0, 'PROCESSING', 'Building knowledge graph...');

  try {
    const response = await fetch(`${API_URL}/api/graph/build`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ siteId: currentSiteId }),
    });

    if (!response.ok) {
      throw new Error('Failed to build knowledge graph');
    }

    const data = await response.json();

    // Fetch summary and display results
    await fetchAndDisplayResults();

  } catch (error) {
    console.error('Build graph error:', error);
    showError('Failed to build knowledge graph. ' + error.message);
    enableForm();
  }
}

// Fetch and display results
async function fetchAndDisplayResults() {
  if (!currentSiteId) return;

  try {
    // Fetch summary statistics
    const summaryResponse = await fetch(`${API_URL}/api/graph/summary?siteId=${currentSiteId}`);

    if (!summaryResponse.ok) {
      throw new Error('Failed to fetch summary');
    }

    const summary = await summaryResponse.json();

    // Fetch pages count
    const pagesResponse = await fetch(`${API_URL}/api/pages?siteId=${currentSiteId}&limit=1`);
    const pagesData = await pagesResponse.json();
    const totalPages = pagesData.pagination?.total || 0;

    // Display results
    displayResults(summary, totalPages);
    hideProgress();
    showResults();
    enableForm();

  } catch (error) {
    console.error('Fetch results error:', error);
    showError('Failed to fetch results. ' + error.message);
    enableForm();
  }
}

// Display results in UI
function displayResults(summary, totalPages) {
  // Update summary stats
  document.getElementById('total-pages').textContent = totalPages;
  document.getElementById('total-entities').textContent = summary.totalEntities || 0;
  document.getElementById('total-relationships').textContent = summary.totalRelations || 0;

  // Display entity types
  const entityTypesContainer = document.getElementById('entity-types-container');
  entityTypesContainer.innerHTML = '';

  if (summary.entityCountByType && summary.entityCountByType.length > 0) {
    summary.entityCountByType.forEach(({ type, count }) => {
      const tag = document.createElement('span');
      tag.className = 'entity-tag';
      tag.textContent = `${type}: ${count}`;
      entityTypesContainer.appendChild(tag);
    });
  } else {
    entityTypesContainer.innerHTML = '<p class="text-muted">No entities found</p>';
  }

  // Display top entities
  const topEntitiesContainer = document.getElementById('top-entities-container');
  topEntitiesContainer.innerHTML = '';

  if (summary.mostConnectedEntities && summary.mostConnectedEntities.length > 0) {
    const list = document.createElement('ul');
    list.style.listStyle = 'none';
    list.style.padding = '0';

    summary.mostConnectedEntities.forEach(entity => {
      const li = document.createElement('li');
      li.style.marginBottom = '0.5rem';
      li.innerHTML = `<strong>${entity.name}</strong> (${entity.type}) - ${entity.connectionCount} connections`;
      list.appendChild(li);
    });

    topEntitiesContainer.appendChild(list);
  } else {
    topEntitiesContainer.innerHTML = '<p class="text-muted">No connected entities found</p>';
  }
}

// Export functions
function viewReport() {
  if (!currentSiteId) return;
  window.open(`${API_URL}/api/report/${currentSiteId}`, '_blank');
}

function exportPDF() {
  if (!currentSiteId) return;
  window.location.href = `${API_URL}/api/report/${currentSiteId}/export/pdf`;
}

function exportJSON() {
  if (!currentSiteId) return;
  window.location.href = `${API_URL}/api/report/${currentSiteId}/export/json`;
}

function exportCSV(type) {
  if (!currentSiteId) return;
  window.location.href = `${API_URL}/api/report/${currentSiteId}/export/${type}.csv`;
}

function exportQuestions(format) {
  if (!currentSiteId) return;
  window.location.href = `${API_URL}/api/questions/${currentSiteId}/export/${format}`;
}

// UI Helper functions
function updateProgress(percentage, processed, total, status, message) {
  document.getElementById('progress-bar-fill').style.width = `${percentage}%`;
  document.getElementById('pages-processed').textContent = total > 0
    ? `${processed} / ${total} pages`
    : 'Building knowledge graph...';

  const badge = document.getElementById('status-badge');
  badge.textContent = status;
  badge.className = 'status-badge status-' + status.toLowerCase();

  document.getElementById('progress-message').textContent = message;
}

function showProgress() {
  document.getElementById('progress-container').classList.remove('hidden');
  document.getElementById('error-container').classList.add('hidden');
  document.getElementById('results-container').classList.add('hidden');
}

function hideProgress() {
  document.getElementById('progress-container').classList.add('hidden');
}

function showResults() {
  document.getElementById('results-container').classList.remove('hidden');
  document.getElementById('error-container').classList.add('hidden');
}

function showError(message) {
  document.getElementById('error-message').textContent = message;
  document.getElementById('error-container').classList.remove('hidden');
  document.getElementById('progress-container').classList.add('hidden');
  document.getElementById('results-container').classList.add('hidden');
  stopPolling();
}

function disableForm() {
  document.getElementById('url-input').disabled = true;
  document.getElementById('max-depth').disabled = true;
  document.getElementById('max-pages').disabled = true;
  document.getElementById('start-crawl-btn').disabled = true;
  document.getElementById('start-btn-text').textContent = 'Processing...';
}

function enableForm() {
  document.getElementById('url-input').disabled = false;
  document.getElementById('max-depth').disabled = false;
  document.getElementById('max-pages').disabled = false;
  document.getElementById('start-crawl-btn').disabled = false;
  document.getElementById('start-btn-text').textContent = 'Start Crawl';
}

function resetForm() {
  // Reset state
  currentJobId = null;
  currentSiteId = null;
  stopPolling();

  // Reset UI
  document.getElementById('url-input').value = '';
  document.getElementById('max-depth').value = '3';
  document.getElementById('max-pages').value = '100';

  // Hide all result sections
  document.getElementById('progress-container').classList.add('hidden');
  document.getElementById('error-container').classList.add('hidden');
  document.getElementById('results-container').classList.add('hidden');

  // Enable form
  enableForm();
}

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
  stopPolling();
});
