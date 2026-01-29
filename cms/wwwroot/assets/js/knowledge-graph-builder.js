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
    // Fetch all data in parallel
    const [summaryResponse, pagesResponse, entitiesResponse, relationsResponse, questionsResponse] = await Promise.all([
      fetch(`${API_URL}/api/graph/summary?siteId=${currentSiteId}`),
      fetch(`${API_URL}/api/pages?siteId=${currentSiteId}&limit=1000`),
      fetch(`${API_URL}/api/graph/entities?siteId=${currentSiteId}&limit=1000`),
      fetch(`${API_URL}/api/graph/relations?siteId=${currentSiteId}&limit=1000`),
      fetch(`${API_URL}/api/questions/${currentSiteId}?limit=1000`)
    ]);

    if (!summaryResponse.ok) {
      throw new Error('Failed to fetch summary');
    }

    const summary = await summaryResponse.json();
    const pagesData = await pagesResponse.json();
    const entitiesData = await entitiesResponse.json();
    const relationsData = await relationsResponse.json();
    const questionsData = await questionsResponse.json();

    // Display all sections
    displaySiteOverview(summary);
    displayResults(summary, pagesData.pagination?.total || 0, questionsData.total || 0);
    displayPages(pagesData.pages || []);
    displayEntities(entitiesData.entities || [], summary.entityCountByType || []);
    displayRelationships(relationsData.relations || []);
    displayQuestions(questionsData.questions || []);

    hideProgress();
    showResults();
    enableForm();

  } catch (error) {
    console.error('Fetch results error:', error);
    showError('Failed to fetch results. ' + error.message);
    enableForm();
  }
}

// Display site overview
function displaySiteOverview(summary) {
  const domain = summary.domain || currentSiteId;
  const url = summary.rootUrl || '--';
  const crawlDate = summary.crawlDate ? new Date(summary.crawlDate).toLocaleString() : new Date().toLocaleString();
  const status = summary.status || 'COMPLETED';

  document.getElementById('site-domain').textContent = domain;
  document.getElementById('site-url').textContent = url;
  document.getElementById('crawl-date').textContent = crawlDate;

  const statusBadge = document.getElementById('site-status');
  statusBadge.textContent = status;
  statusBadge.className = 'status-badge status-' + status.toLowerCase();
}

// Display results in UI
function displayResults(summary, totalPages, totalQuestions) {
  // Update summary stats
  document.getElementById('total-pages').textContent = totalPages;
  document.getElementById('total-entities').textContent = summary.totalEntities || 0;
  document.getElementById('total-relationships').textContent = summary.totalRelations || 0;
  document.getElementById('total-questions').textContent = totalQuestions;
  document.getElementById('max-depth-reached').textContent = summary.maxDepthReached || '--';
  document.getElementById('crawl-duration').textContent = summary.duration || '--';

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

// Display pages
function displayPages(pages) {
  const tbody = document.getElementById('pages-table-body');
  tbody.innerHTML = '';

  if (pages.length === 0) {
    tbody.innerHTML = '<tr><td colspan="5" class="text-center">No pages found</td></tr>';
    return;
  }

  pages.forEach(page => {
    const row = document.createElement('tr');
    const url = page.url || '--';
    const title = page.title || 'Untitled';
    const depth = page.depth ?? '--';
    const entityCount = page.entityCount || 0;
    const status = page.status || 'SUCCESS';

    row.innerHTML = `
      <td><a href="${url}" target="_blank" style="color: var(--accent-blue); text-decoration: none;">${truncate(url, 60)}</a></td>
      <td>${truncate(title, 50)}</td>
      <td>${depth}</td>
      <td>${entityCount}</td>
      <td><span class="status-badge status-${status.toLowerCase()}">${status}</span></td>
    `;
    tbody.appendChild(row);
  });
}

// Display entities
function displayEntities(entities, entityTypes) {
  // Display entity type distribution
  const entityTypesContainer = document.getElementById('entity-types-container');
  entityTypesContainer.innerHTML = '';

  if (entityTypes && entityTypes.length > 0) {
    entityTypes.forEach(({ type, count }) => {
      const tag = document.createElement('span');
      tag.className = 'entity-tag';
      tag.textContent = `${type}: ${count}`;
      entityTypesContainer.appendChild(tag);
    });
  } else {
    entityTypesContainer.innerHTML = '<p class="text-muted">No entities found</p>';
  }

  // Display entities table
  const tbody = document.getElementById('entities-table-body');
  tbody.innerHTML = '';

  if (entities.length === 0) {
    tbody.innerHTML = '<tr><td colspan="5" class="text-center">No entities found</td></tr>';
    return;
  }

  entities.forEach(entity => {
    const row = document.createElement('tr');
    const name = entity.name || '--';
    const type = entity.type || '--';
    const confidence = entity.confidence || 0;
    const mentions = entity.mentions || 0;
    const relationshipCount = entity.relationshipCount || 0;

    const confidenceClass = confidence >= 0.8 ? 'confidence-high' : confidence >= 0.5 ? 'confidence-medium' : 'confidence-low';

    row.innerHTML = `
      <td><strong>${name}</strong></td>
      <td><span class="entity-tag">${type}</span></td>
      <td><span class="confidence-badge ${confidenceClass}">${(confidence * 100).toFixed(0)}%</span></td>
      <td>${mentions}</td>
      <td>${relationshipCount}</td>
    `;
    tbody.appendChild(row);
  });
}

// Display relationships
function displayRelationships(relations) {
  const tbody = document.getElementById('relationships-table-body');
  tbody.innerHTML = '';

  if (relations.length === 0) {
    tbody.innerHTML = '<tr><td colspan="4" class="text-center">No relationships found</td></tr>';
    return;
  }

  relations.forEach(relation => {
    const row = document.createElement('tr');
    const sourceEntity = relation.sourceEntity || '--';
    const relationType = relation.type || '--';
    const targetEntity = relation.targetEntity || '--';
    const confidence = relation.confidence || 0;

    const confidenceClass = confidence >= 0.8 ? 'confidence-high' : confidence >= 0.5 ? 'confidence-medium' : 'confidence-low';

    row.innerHTML = `
      <td><strong>${sourceEntity}</strong></td>
      <td><span class="entity-tag">${relationType}</span></td>
      <td><strong>${targetEntity}</strong></td>
      <td><span class="confidence-badge ${confidenceClass}">${(confidence * 100).toFixed(0)}%</span></td>
    `;
    tbody.appendChild(row);
  });
}

// Display questions
function displayQuestions(questions) {
  const container = document.getElementById('questions-container');
  container.innerHTML = '';

  if (questions.length === 0) {
    container.innerHTML = '<p class="text-muted">No questions generated</p>';
    return;
  }

  // Group questions by type
  const groupedQuestions = {};
  questions.forEach(q => {
    const type = q.type || 'General';
    if (!groupedQuestions[type]) {
      groupedQuestions[type] = [];
    }
    groupedQuestions[type].push(q);
  });

  // Display grouped questions
  Object.keys(groupedQuestions).forEach(type => {
    const group = document.createElement('div');
    group.className = 'question-group';

    const header = document.createElement('div');
    header.className = 'question-group-header';
    header.textContent = `${type} (${groupedQuestions[type].length})`;
    group.appendChild(header);

    groupedQuestions[type].forEach(q => {
      const item = document.createElement('div');
      item.className = 'question-item';

      const questionText = document.createElement('div');
      questionText.className = 'question-text';
      questionText.textContent = q.question || '--';
      item.appendChild(questionText);

      if (q.answer) {
        const answer = document.createElement('div');
        answer.className = 'question-answer';
        answer.textContent = q.answer;
        item.appendChild(answer);
      }

      group.appendChild(item);
    });

    container.appendChild(group);
  });
}

// Helper function to truncate text
function truncate(text, maxLength) {
  if (text.length <= maxLength) return text;
  return text.substring(0, maxLength) + '...';
}

// Toggle section visibility
function toggleSection(sectionId) {
  const section = document.getElementById(sectionId);
  if (section.style.display === 'none') {
    section.style.display = 'block';
  } else {
    section.style.display = 'none';
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
