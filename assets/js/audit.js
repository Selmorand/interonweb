/**
 * AI Readiness Platform - Audit Functionality
 * Connects to the audit API at ai-auditor.interon.co.za
 */

const API_URL = 'https://ai-auditor.interon.co.za';

// State
let currentResults = null;

// Run audit
async function runAudit() {
  const urlInput = document.getElementById('url-input');
  const url = urlInput.value.trim();

  // Validate URL
  if (!url) {
    showError('Please enter a website URL.');
    return;
  }

  // Add protocol if missing
  let auditUrl = url;
  if (!url.startsWith('http://') && !url.startsWith('https://')) {
    auditUrl = 'https://' + url;
  }

  // Validate URL format
  try {
    new URL(auditUrl);
  } catch {
    showError('Please enter a valid URL (e.g., https://example.com)');
    return;
  }

  // Show loading
  showLoading();

  try {
    const response = await fetch(`${API_URL}/api/analyze`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ url: auditUrl }),
    });

    const data = await response.json();

    if (!response.ok || !data.success) {
      throw new Error(data.error || 'Failed to run audit');
    }

    // API returns data in data.data
    const result = data.data;
    currentResults = result;
    displayResults(result);
  } catch (error) {
    console.error('Audit error:', error);
    showError(error.message || 'Unable to complete audit. Please try again.');
  }
}

// Show loading state
function showLoading() {
  document.getElementById('audit-form-container').classList.add('hidden');
  document.getElementById('loading').classList.remove('hidden');
  document.getElementById('error').classList.add('hidden');
  document.getElementById('results').classList.add('hidden');
}

// Show error state
function showError(message) {
  document.getElementById('audit-form-container').classList.add('hidden');
  document.getElementById('loading').classList.add('hidden');
  document.getElementById('error').classList.remove('hidden');
  document.getElementById('results').classList.add('hidden');
  document.getElementById('error-message').textContent = message;
}

// Reset to form
function resetAudit() {
  document.getElementById('audit-form-container').classList.remove('hidden');
  document.getElementById('loading').classList.add('hidden');
  document.getElementById('error').classList.add('hidden');
  document.getElementById('results').classList.add('hidden');
  document.getElementById('url-input').value = '';
  currentResults = null;
}

// Helper: Safe element setter
function setElementText(id, text) {
  const el = document.getElementById(id);
  if (el) el.textContent = text;
}

function setElementHTML(id, html) {
  const el = document.getElementById(id);
  if (el) el.innerHTML = html;
}

function setElementClass(id, className) {
  const el = document.getElementById(id);
  if (el) el.className = className;
}

function showElement(id) {
  const el = document.getElementById(id);
  if (el) el.classList.remove('hidden');
}

function hideElement(id) {
  const el = document.getElementById(id);
  if (el) el.classList.add('hidden');
}

// Display results
function displayResults(result) {
  hideElement('audit-form-container');
  hideElement('loading');
  hideElement('error');
  showElement('results');

  // Header info
  setElementText('result-url', result.url);
  setElementText('load-time', result.loadTime + 'ms');

  // Overall score
  const overallScore = result.scores?.overall?.score || 0;
  const overallLevel = result.scores?.overall?.level || 'unknown';

  setElementText('score-value', overallScore);
  setElementText('score-level', overallLevel);
  setElementClass('score-level', 'score-level ' + getScoreClass(overallScore));

  // Update score ring
  const ring = document.getElementById('score-ring-progress');
  if (ring) {
    const circumference = 2 * Math.PI * 85;
    ring.style.strokeDasharray = circumference;
    ring.style.strokeDashoffset = circumference * (1 - overallScore / 100);
    ring.classList.add(getScoreClass(overallScore));
  }

  // Update overall card color
  setElementClass('overall-score-card', 'card card-score mb-4 ' + getScoreBgClass(overallScore));

  // AI Readiness score
  if (result.scores?.aiReadiness) {
    const ai = result.scores.aiReadiness;
    setElementText('ai-score', ai.score);
    setElementText('ai-level', ai.level);
    setElementClass('ai-level', 'score-card-level ' + getScoreClass(ai.score));
    setElementText('ai-interpretation', ai.interpretation || '');

    if (ai.breakdown) {
      setElementHTML('ai-breakdown', renderBreakdown([
        { label: 'Structured Data', value: ai.breakdown.structuredDataPresence, max: 30 },
        { label: 'Service Clarity', value: ai.breakdown.serviceClarity, max: 25 },
        { label: 'Machine Intent', value: ai.breakdown.machineReadableIntent, max: 25 },
        { label: 'Content Quality', value: ai.breakdown.contentQuality, max: 20 }
      ], 'blue'));
    }
  }

  // SEO score
  if (result.scores?.seo) {
    const seo = result.scores.seo;
    setElementText('seo-score', seo.score);
    setElementText('seo-level', seo.level);
    setElementClass('seo-level', 'score-card-level ' + getScoreClass(seo.score));
    setElementText('seo-interpretation', seo.interpretation || '');

    if (seo.breakdown) {
      setElementHTML('seo-breakdown', renderBreakdown([
        { label: 'Metadata', value: seo.breakdown.metadataQuality, max: 30 },
        { label: 'Page Structure', value: seo.breakdown.pageStructure, max: 30 },
        { label: 'Technical SEO', value: seo.breakdown.technicalSEO, max: 20 }
      ], 'green'));
    }
  }

  // GEO score
  if (result.scores?.geo) {
    const geo = result.scores.geo;
    setElementText('geo-score', geo.score);
    setElementText('geo-level', geo.level);
    setElementClass('geo-level', 'score-card-level ' + getScoreClass(geo.score));
    setElementText('geo-interpretation', geo.interpretation || '');

    if (geo.breakdown) {
      setElementHTML('geo-breakdown', renderBreakdown([
        { label: 'Answer-Friendliness', value: geo.breakdown.answerFriendliness, max: 30 },
        { label: 'Entity Clarity', value: geo.breakdown.entityClarity, max: 30 },
        { label: 'Agent Formatting', value: geo.breakdown.agentFriendlyFormatting, max: 20 },
        { label: 'Ambiguity Reduction', value: geo.breakdown.ambiguityReduction, max: 20 }
      ], 'purple'));
    }
  }

  // AI Insights
  if (result.llmAnalysis) {
    showElement('ai-insights');
    const insights = [
      { title: 'Service Clarity', data: result.llmAnalysis.serviceClarity, color: 'blue', icon: '&#127919;' },
      { title: 'Entity Clarity', data: result.llmAnalysis.entityClarity, color: 'purple', icon: '&#127970;' },
      { title: 'Content Consistency', data: result.llmAnalysis.contentConsistency, color: 'green', icon: '&#10003;' },
      { title: 'Answer-Friendliness', data: result.llmAnalysis.answerFriendliness, color: 'orange', icon: '&#128172;' }
    ];

    setElementHTML('insights-grid', insights.map(insight => `
      <div class="insight-card">
        <div class="insight-header">
          <span class="insight-icon">${insight.icon}</span>
          <span class="insight-title">${insight.title}</span>
          <span class="insight-score insight-score-${insight.color}">${insight.data?.score || '--'}</span>
        </div>
        <p class="insight-reasoning">${escapeHtml(insight.data?.reasoning || '')}</p>
      </div>
    `).join(''));
  }

  // Schema Analysis
  if (result.schemaAudit) {
    showElement('schema-section');
    const schema = result.schemaAudit;

    setElementText('schema-score', schema.overallScore || '--');

    // Can AI List Services
    const canList = schema.canAIListServices;
    if (canList) {
      const answerClass = canList.answer === 'yes' ? 'success' : canList.answer === 'partially' ? 'warning' : 'error';
      const answerIcon = canList.answer === 'yes' ? '&#10003;' : canList.answer === 'partially' ? '&#9888;' : '&#10007;';
      const answerText = canList.answer === 'yes' ? 'Yes' : canList.answer === 'partially' ? 'Partially' : 'No';

      setElementHTML('ai-services-check', `
        <div class="ai-services-box ai-services-${answerClass}">
          <span class="ai-services-icon">${answerIcon}</span>
          <div class="ai-services-content">
            <h4>Can AI confidently list your services?</h4>
            <p class="ai-services-answer">${answerText} <span class="confidence">(${canList.confidence}% confidence)</span></p>
            <p class="ai-services-reasoning">${escapeHtml(canList.reasoning)}</p>
          </div>
        </div>
      `);
    }

    // Present types
    const presentTypes = schema.presentTypes || [];
    setElementHTML('present-types', presentTypes.length > 0
      ? presentTypes.map(t => `<span class="tag tag-success">${escapeHtml(t)}</span>`).join('')
      : '<p class="text-muted">No schema types detected</p>');

    // Missing types
    const missingTypes = schema.missingCriticalTypes || [];
    setElementHTML('missing-types', missingTypes.length > 0
      ? missingTypes.map(t => `<span class="tag tag-error">${escapeHtml(t)}</span>`).join('')
      : '<p class="text-success">All critical types present!</p>');

    // Schema assessments
    const assessments = [];
    if (schema.organizationClarity) {
      assessments.push({
        title: 'Organization',
        subtitle: 'Who is your business?',
        score: schema.organizationClarity.score,
        items: [
          { label: 'Business name', ok: schema.organizationClarity.nameFound },
          { label: 'Description', ok: schema.organizationClarity.descriptionFound },
          { label: 'Website URL', ok: schema.organizationClarity.urlFound },
          { label: 'Logo', ok: schema.organizationClarity.logoFound },
          { label: 'Contact info', ok: schema.organizationClarity.contactFound }
        ]
      });
    }
    if (schema.serviceClarity) {
      assessments.push({
        title: 'Services',
        subtitle: 'What does your business do?',
        score: schema.serviceClarity.score,
        items: [
          { label: 'Service schema', ok: schema.serviceClarity.hasServiceSchema },
          { label: 'Offer schema', ok: schema.serviceClarity.hasOfferSchema },
          { label: 'Offer catalog', ok: schema.serviceClarity.hasOfferCatalog }
        ],
        extra: `<strong>${schema.serviceClarity.servicesInSchema || 0}</strong> services in schema`
      });
    }
    if (schema.identityConsistency) {
      const ic = schema.identityConsistency;
      assessments.push({
        title: 'Consistency',
        subtitle: 'Is your identity consistent?',
        score: ic.score,
        custom: `
          ${ic.schemaName ? `<p><span class="text-muted">Schema:</span> ${escapeHtml(ic.schemaName)}</p>` : ''}
          ${ic.h1Text ? `<p><span class="text-muted">H1:</span> ${escapeHtml(ic.h1Text)}</p>` : ''}
          <p class="${ic.isConsistent ? 'text-success' : 'text-error'}">${ic.isConsistent ? '&#10003; Identity is consistent' : '&#10007; Identity has conflicts'}</p>
        `
      });
    }

    setElementHTML('schema-assessments', assessments.map(a => `
      <div class="assessment-card">
        <div class="assessment-header">
          <h4>${a.title}</h4>
          <span class="assessment-score ${getScoreClass(a.score)}">${a.score}%</span>
        </div>
        <p class="assessment-subtitle">${a.subtitle}</p>
        ${a.items ? a.items.map(i => `<p class="${i.ok ? 'text-success' : 'text-error'}">${i.ok ? '&#10003;' : '&#10007;'} ${i.label}</p>`).join('') : ''}
        ${a.extra ? `<p class="mt-2">${a.extra}</p>` : ''}
        ${a.custom || ''}
      </div>
    `).join(''));

    // Schema improvements
    if (schema.topImprovements && schema.topImprovements.length > 0) {
      setElementHTML('schema-improvements', `
        <h4 class="mb-2">Top Schema Improvements</h4>
        ${schema.topImprovements.map(imp => `
          <div class="improvement-item improvement-${imp.impact}">
            <span class="improvement-badge">${imp.impact.toUpperCase()}</span>
            <div>
              <strong>${escapeHtml(imp.title)}</strong>
              <p>${escapeHtml(imp.explanation)}</p>
            </div>
          </div>
        `).join('')}
      `);
    }
  }

  // Recommendations
  if (result.recommendations && result.recommendations.length > 0) {
    showElement('recommendations-section');
    setElementHTML('recommendations', result.recommendations.map(rec => {
      const severityClass = rec.severity === 'High' ? 'error' : rec.severity === 'Medium' ? 'warning' : 'info';
      const severityIcon = rec.severity === 'High' ? '&#128308;' : rec.severity === 'Medium' ? '&#128993;' : '&#128994;';
      const difficultyClass = rec.difficulty === 'Easy' ? 'success' : rec.difficulty === 'Medium' ? 'warning' : 'error';

      return `
        <div class="recommendation-card recommendation-${severityClass}" data-pdf-section="recommendation">
          <div class="recommendation-header">
            <div class="recommendation-title-row">
              <span class="recommendation-icon">${severityIcon}</span>
              <h4>${escapeHtml(rec.title)}</h4>
              <span class="tag tag-${severityClass}">${rec.severity}</span>
            </div>
            <div class="recommendation-points">
              <span class="points-value">+${rec.pointsGained}</span>
              <span class="points-label">points</span>
            </div>
          </div>

          <div class="recommendation-categories">
            ${rec.category.map(c => `<span class="tag tag-neutral">${escapeHtml(c)}</span>`).join('')}
          </div>

          <div class="recommendation-body">
            <div class="recommendation-impact">
              <strong>Impact:</strong> ${escapeHtml(rec.impact)}
            </div>

            <div class="recommendation-difficulty">
              <span>Difficulty: <span class="tag tag-${difficultyClass}">${rec.difficulty}</span></span>
              <p>${escapeHtml(rec.difficultyExplanation)}</p>
            </div>

            <div class="recommendation-action">
              <strong>&#10003; What to do:</strong>
              <p>${escapeHtml(rec.whatToDo)}</p>
            </div>

            <div class="recommendation-score-info">
              <span>Current: ${rec.currentScore}/${rec.maxScore}</span>
              <span>&#8594;</span>
              <span class="text-success">Potential: ${Math.min(rec.currentScore + rec.pointsGained, rec.maxScore)}/${rec.maxScore}</span>
            </div>
          </div>
        </div>
      `;
    }).join(''));
  }
}

// Render breakdown bars
function renderBreakdown(items, color) {
  return `<div class="breakdown-list">
    ${items.map(item => `
      <div class="breakdown-item">
        <div class="breakdown-header">
          <span>${item.label}</span>
          <span class="breakdown-value">${Math.round(item.value || 0)}/${item.max}</span>
        </div>
        <div class="breakdown-bar">
          <div class="breakdown-fill breakdown-fill-${color}" style="width: ${((item.value || 0) / item.max) * 100}%"></div>
        </div>
      </div>
    `).join('')}
  </div>`;
}

// Helper: Get score class
function getScoreClass(score) {
  if (score >= 90) return 'score-excellent';
  if (score >= 75) return 'score-good';
  if (score >= 50) return 'score-fair';
  return 'score-poor';
}

// Helper: Get score bg class
function getScoreBgClass(score) {
  if (score >= 90) return 'card-score-excellent';
  if (score >= 75) return 'card-score-good';
  if (score >= 50) return 'card-score-fair';
  return 'card-score-poor';
}

// Helper: Escape HTML
function escapeHtml(text) {
  if (!text) return '';
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

// Export to PDF - captures HTML sections as images
async function exportPDF() {
  if (!currentResults) return;

  const btn = document.getElementById('export-pdf-btn');
  const btnText = document.getElementById('export-pdf-text');

  btn.disabled = true;
  btnText.textContent = 'Generating PDF...';

  try {
    const { jsPDF } = window.jspdf;
    const reportElement = document.getElementById('report-content');

    if (!reportElement) {
      console.error('Report content not found');
      return;
    }

    // Get all sections marked for PDF (excluding individual recommendation cards for now)
    const allSections = reportElement.querySelectorAll('[data-pdf-section]');
    // Filter to only visible sections, separating main sections from recommendation cards
    const mainSections = [];
    const recommendationCards = [];

    Array.from(allSections).forEach(section => {
      const isHidden = section.classList.contains('hidden');
      const rect = section.getBoundingClientRect();
      const hasSize = rect.width > 0 && rect.height > 0;
      const sectionType = section.getAttribute('data-pdf-section');

      if (!isHidden && hasSize) {
        if (sectionType === 'recommendation') {
          recommendationCards.push(section);
        } else if (sectionType !== 'recommendations') {
          // Skip the recommendations container, we'll use individual cards
          mainSections.push(section);
        }
      }
    });

    console.log(`Found ${mainSections.length} main sections and ${recommendationCards.length} recommendation cards`);

    if (mainSections.length === 0 && recommendationCards.length === 0) {
      console.error('No PDF sections found');
      alert('No sections to export. Please run an audit first.');
      return;
    }

    const pdf = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    const pdfWidth = pdf.internal.pageSize.getWidth();
    const pdfHeight = pdf.internal.pageSize.getHeight();
    const headerHeight = 15;
    const footerHeight = 12;
    const margin = 8;
    const contentWidth = pdfWidth - (margin * 2);
    const maxContentHeight = pdfHeight - headerHeight - footerHeight;
    const sectionGap = 4;

    // Load logo for footer
    let logoData = null;
    let logoAspectRatio = 3.5; // Default fallback
    try {
      const logoImg = new Image();
      logoImg.crossOrigin = 'anonymous';
      await new Promise((resolve, reject) => {
        logoImg.onload = resolve;
        logoImg.onerror = reject;
        logoImg.src = '/assets/images/logo.png';
      });
      logoAspectRatio = logoImg.width / logoImg.height;
      const logoCanvas = document.createElement('canvas');
      logoCanvas.width = logoImg.width;
      logoCanvas.height = logoImg.height;
      const ctx = logoCanvas.getContext('2d');
      ctx.drawImage(logoImg, 0, 0);
      logoData = logoCanvas.toDataURL('image/png');
      console.log(`Logo loaded: ${logoImg.width}x${logoImg.height}, aspect ratio: ${logoAspectRatio}`);
    } catch (e) {
      console.warn('Could not load logo for PDF:', e);
    }

    // Load white logo for sales page header
    let whiteLogoData = null;
    let whiteLogoAspectRatio = logoAspectRatio;
    try {
      const whiteLogoImg = new Image();
      whiteLogoImg.crossOrigin = 'anonymous';
      await new Promise((resolve, reject) => {
        whiteLogoImg.onload = resolve;
        whiteLogoImg.onerror = reject;
        whiteLogoImg.src = '/assets/images/NewLogoWhite 2.png';
      });
      whiteLogoAspectRatio = whiteLogoImg.width / whiteLogoImg.height;
      const whiteLogoCanvas = document.createElement('canvas');
      whiteLogoCanvas.width = whiteLogoImg.width;
      whiteLogoCanvas.height = whiteLogoImg.height;
      const ctx = whiteLogoCanvas.getContext('2d');
      ctx.drawImage(whiteLogoImg, 0, 0);
      whiteLogoData = whiteLogoCanvas.toDataURL('image/png');
      console.log(`White logo loaded: ${whiteLogoImg.width}x${whiteLogoImg.height}, aspect ratio: ${whiteLogoAspectRatio}`);
    } catch (e) {
      console.warn('Could not load white logo for PDF:', e);
      whiteLogoData = logoData; // Fall back to regular logo
    }

    // Add PDF capture mode class for light backgrounds
    reportElement.classList.add('pdf-capture-mode');

    // Helper function to capture a section
    const captureSection = async (section, sectionName) => {
      const canvas = await html2canvas(section, {
        scale: 1.5,
        useCORS: true,
        logging: false,
        backgroundColor: '#ffffff',
        windowWidth: 1200,
        scrollY: -window.scrollY,
        onclone: (clonedDoc) => {
          const clonedReport = clonedDoc.getElementById('report-content');
          if (clonedReport) {
            clonedReport.classList.add('pdf-capture-mode');
          }
          clonedDoc.body.classList.add('pdf-capture-mode');
        }
      });

      if (canvas.width === 0 || canvas.height === 0) {
        console.warn(`Section ${sectionName} has zero dimensions, skipping`);
        return null;
      }

      const imgWidth = contentWidth;
      const imgHeight = (canvas.height * contentWidth) / canvas.width;

      console.log(`Section ${sectionName}: canvas ${canvas.width}x${canvas.height}, pdf height: ${imgHeight}mm`);

      return {
        canvas,
        width: imgWidth,
        height: imgHeight,
        name: sectionName,
        isRecommendation: sectionName === 'recommendation'
      };
    };

    // Capture all main sections first
    const sectionData = [];

    for (const section of mainSections) {
      const sectionName = section.getAttribute('data-pdf-section');
      console.log(`Capturing main section: ${sectionName}`);

      try {
        const data = await captureSection(section, sectionName);
        if (data) sectionData.push(data);
      } catch (err) {
        console.error('Failed to capture section:', sectionName, err);
      }
    }

    // Capture recommendation cards individually
    for (let i = 0; i < recommendationCards.length; i++) {
      const card = recommendationCards[i];
      console.log(`Capturing recommendation card ${i + 1}/${recommendationCards.length}`);

      try {
        const data = await captureSection(card, 'recommendation');
        if (data) sectionData.push(data);
      } catch (err) {
        console.error('Failed to capture recommendation card:', i, err);
      }
    }

    // Remove PDF capture mode class
    reportElement.classList.remove('pdf-capture-mode');

    if (sectionData.length === 0) {
      throw new Error('No sections could be captured');
    }

    // Calculate total pages needed (+ 1 for the final sales/CTA page)
    let totalHeight = sectionData.reduce((sum, s) => sum + s.height + sectionGap, 0);
    const pageCount = Math.ceil(totalHeight / maxContentHeight) + 1;

    // Helper functions
    const addHeader = (pageNum) => {
      pdf.setFillColor(59, 130, 246);
      pdf.rect(0, 0, pdfWidth, 12, 'F');
      pdf.setTextColor(255, 255, 255);
      pdf.setFontSize(10);
      pdf.setFont('helvetica', 'bold');
      pdf.text(`Audit for ${currentResults.url}`, margin, 8);
      pdf.setFontSize(8);
      pdf.setFont('helvetica', 'normal');
      pdf.text(`Page ${pageNum} of ${pageCount}`, pdfWidth - margin - 22, 8);
      pdf.setTextColor(0, 0, 0);
    };

    const addFooter = () => {
      // Add logo on the right side of footer
      if (logoData) {
        const logoHeight = 5;
        const logoWidth = logoHeight * logoAspectRatio;
        pdf.addImage(logoData, 'PNG', pdfWidth - margin - logoWidth, pdfHeight - 8, logoWidth, logoHeight);
      }

      pdf.setFontSize(7);
      pdf.setTextColor(128, 128, 128);
      pdf.text(
        `Generated on ${new Date().toLocaleDateString()} | AI Website Readiness Auditor`,
        margin,
        pdfHeight - 5
      );
      pdf.setTextColor(0, 0, 0);
    };

    // Place sections on pages
    let currentPage = 1;
    let currentY = headerHeight;

    addHeader(currentPage);
    addFooter();

    console.log(`Placing ${sectionData.length} sections on PDF pages`);

    // Helper to slice a canvas into a portion
    const sliceCanvas = (sourceCanvas, startY, sliceHeight) => {
      const slicedCanvas = document.createElement('canvas');
      slicedCanvas.width = sourceCanvas.width;
      slicedCanvas.height = sliceHeight;
      const ctx = slicedCanvas.getContext('2d');
      ctx.drawImage(
        sourceCanvas,
        0, startY, sourceCanvas.width, sliceHeight,  // source
        0, 0, sourceCanvas.width, sliceHeight         // destination
      );
      return slicedCanvas;
    };

    for (const section of sectionData) {
      console.log(`Placing section ${section.name}, height: ${section.height}mm, currentY: ${currentY}mm, isRecommendation: ${section.isRecommendation}`);

      const availableHeight = pdfHeight - footerHeight - currentY;

      // For recommendation cards: never split, always keep whole
      if (section.isRecommendation) {
        // If card doesn't fit on current page, start a new page
        if (section.height > availableHeight && currentY > headerHeight) {
          pdf.addPage();
          currentPage++;
          console.log(`Starting new page ${currentPage} for recommendation card`);
          addHeader(currentPage);
          addFooter();
          currentY = headerHeight;
        }

        // Add the card (whole, never sliced)
        const imgData = section.canvas.toDataURL('image/jpeg', 0.85);
        pdf.addImage(
          imgData,
          'JPEG',
          margin,
          currentY,
          section.width,
          section.height
        );
        currentY += section.height + sectionGap;
        continue;
      }

      // For non-recommendation sections: check if it fits on current page
      if (section.height > availableHeight && currentY > headerHeight) {
        // Start new page
        pdf.addPage();
        currentPage++;
        console.log(`Starting new page ${currentPage}`);
        addHeader(currentPage);
        addFooter();
        currentY = headerHeight;
      }

      // If section is taller than max content height, split it across multiple pages
      if (section.height > maxContentHeight) {
        console.log(`Section ${section.name} is larger than a page (${section.height}mm > ${maxContentHeight}mm), splitting...`);

        const pixelsPerMM = section.canvas.height / section.height;
        let remainingCanvasHeight = section.canvas.height;
        let sourceY = 0;

        while (remainingCanvasHeight > 0) {
          const pageAvailableHeight = pdfHeight - footerHeight - currentY;
          const availableCanvasHeight = Math.floor(pageAvailableHeight * pixelsPerMM);
          const sliceHeightPx = Math.min(remainingCanvasHeight, availableCanvasHeight);
          const sliceHeightMM = sliceHeightPx / pixelsPerMM;

          // Slice the canvas
          const slicedCanvas = sliceCanvas(section.canvas, sourceY, sliceHeightPx);
          const imgData = slicedCanvas.toDataURL('image/jpeg', 0.85);

          // Add sliced image
          pdf.addImage(
            imgData,
            'JPEG',
            margin,
            currentY,
            section.width,
            sliceHeightMM
          );

          remainingCanvasHeight -= sliceHeightPx;
          sourceY += sliceHeightPx;
          currentY += sliceHeightMM;

          // If more content remains, start new page
          if (remainingCanvasHeight > 0) {
            pdf.addPage();
            currentPage++;
            console.log(`Starting new page ${currentPage} for continued section`);
            addHeader(currentPage);
            addFooter();
            currentY = headerHeight;
          }
        }
      } else {
        // Section fits on page, add normally
        const imgData = section.canvas.toDataURL('image/jpeg', 0.85);
        pdf.addImage(
          imgData,
          'JPEG',
          margin,
          currentY,
          section.width,
          section.height
        );
        currentY += section.height + sectionGap;
      }
    }

    // Add final sales/CTA page - CREATIVE DESIGN
    pdf.addPage();
    currentPage++;

    // Large gradient-style header block
    pdf.setFillColor(30, 58, 138); // Deep blue
    pdf.rect(0, 0, pdfWidth, 75, 'F');

    // Accent stripe
    pdf.setFillColor(249, 115, 22); // Orange accent
    pdf.rect(0, 72, pdfWidth, 3, 'F');

    let ctaY = 20;

    // White logo in header
    if (whiteLogoData) {
      const bigLogoHeight = 12;
      const bigLogoWidth = bigLogoHeight * whiteLogoAspectRatio;
      pdf.addImage(whiteLogoData, 'PNG', (pdfWidth - bigLogoWidth) / 2, ctaY, bigLogoWidth, bigLogoHeight);
      ctaY += bigLogoHeight + 8;
    }

    // Big headline in header
    pdf.setFontSize(22);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(255, 255, 255);
    pdf.text('Don\'t Let AI Forget You', pdfWidth / 2, ctaY + 5, { align: 'center' });
    ctaY += 15;

    pdf.setFontSize(11);
    pdf.setFont('helvetica', 'normal');
    pdf.setTextColor(191, 219, 254); // Light blue
    pdf.text('Your competitors are already optimising. Are you?', pdfWidth / 2, ctaY + 5, { align: 'center' });

    ctaY = 90;

    // The Problem - with icon
    pdf.setFillColor(254, 242, 242); // Light red bg
    pdf.roundedRect(margin, ctaY, contentWidth, 35, 2, 2, 'F');

    pdf.setFontSize(9);
    pdf.setTextColor(185, 28, 28);
    pdf.text('THE PROBLEM', margin + 5, ctaY + 8);

    pdf.setFontSize(12);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(127, 29, 29);
    pdf.text('AI Can\'t Find Your Business', margin + 5, ctaY + 17);

    pdf.setFontSize(9);
    pdf.setFont('helvetica', 'normal');
    pdf.setTextColor(75, 85, 99);
    const prob = 'ChatGPT, Claude, Perplexity - they\'re the new search. But without proper optimisation, they don\'t know you exist.';
    const probLines = pdf.splitTextToSize(prob, contentWidth - 10);
    pdf.text(probLines, margin + 5, ctaY + 25);

    ctaY += 42;

    // What You're Missing - with bullets
    pdf.setFillColor(255, 251, 235); // Light amber bg
    pdf.roundedRect(margin, ctaY, contentWidth, 40, 2, 2, 'F');

    pdf.setFontSize(9);
    pdf.setTextColor(180, 83, 9);
    pdf.text('WHAT YOU\'RE MISSING', margin + 5, ctaY + 8);

    pdf.setFontSize(10);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(146, 64, 14);

    const bullets = [
      'Customers asking AI for recommendations in your industry',
      'AI-generated answers that could feature YOUR business',
      'A competitive edge that grows more valuable every day'
    ];

    let bulletY = ctaY + 17;
    bullets.forEach(b => {
      pdf.setTextColor(217, 119, 6);
      pdf.text('>', margin + 5, bulletY);
      pdf.setTextColor(75, 85, 99);
      pdf.setFont('helvetica', 'normal');
      pdf.text(b, margin + 12, bulletY);
      bulletY += 7;
    });

    ctaY += 47;

    // The Solution - green box
    pdf.setFillColor(240, 253, 244); // Light green bg
    pdf.roundedRect(margin, ctaY, contentWidth, 38, 2, 2, 'F');

    pdf.setFontSize(9);
    pdf.setTextColor(21, 128, 61);
    pdf.text('THE SOLUTION', margin + 5, ctaY + 8);

    pdf.setFontSize(13);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(22, 101, 52);
    pdf.text('We Make AI Recommend You', margin + 5, ctaY + 18);

    pdf.setFontSize(9);
    pdf.setFont('helvetica', 'normal');
    pdf.setTextColor(75, 85, 99);
    const sol = 'Schema markup, AI-optimised content, GEO signals - we implement everything in this report so AI systems find, understand, and recommend your business.';
    const solLines = pdf.splitTextToSize(sol, contentWidth - 10);
    pdf.text(solLines, margin + 5, ctaY + 26);

    ctaY += 48;

    // Big CTA Box
    pdf.setFillColor(59, 130, 246); // Blue
    pdf.roundedRect(margin, ctaY, contentWidth, 55, 4, 4, 'F');

    pdf.setFontSize(16);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(255, 255, 255);
    pdf.text('Ready to Get Found?', pdfWidth / 2, ctaY + 14, { align: 'center' });

    pdf.setFontSize(10);
    pdf.setFont('helvetica', 'normal');
    pdf.setTextColor(219, 234, 254);
    pdf.text('Book your free consultation today', pdfWidth / 2, ctaY + 23, { align: 'center' });

    // Contact row
    const contactY = ctaY + 35;

    // Email box
    pdf.setFillColor(255, 255, 255);
    pdf.roundedRect(margin + 8, contactY, 55, 12, 2, 2, 'F');
    pdf.setFontSize(8);
    pdf.setTextColor(59, 130, 246);
    pdf.text('EMAIL', margin + 12, contactY + 4);
    pdf.setFontSize(8);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(30, 64, 175);
    pdf.text('hello@interon.co.za', margin + 12, contactY + 9);

    // Phone box
    pdf.setFillColor(255, 255, 255);
    pdf.roundedRect(margin + 68, contactY, 50, 12, 2, 2, 'F');
    pdf.setFontSize(8);
    pdf.setFont('helvetica', 'normal');
    pdf.setTextColor(59, 130, 246);
    pdf.text('PHONE', margin + 72, contactY + 4);
    pdf.setFontSize(8);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(30, 64, 175);
    pdf.text('+27 83 326 9469', margin + 72, contactY + 9);

    // WhatsApp button
    pdf.setFillColor(37, 211, 102);
    const whatsappBtnWidth = 50;
    const whatsappBtnX = margin + 123;
    pdf.roundedRect(whatsappBtnX, contactY, whatsappBtnWidth, 12, 2, 2, 'F');
    pdf.setTextColor(255, 255, 255);
    pdf.setFontSize(9);
    pdf.setFont('helvetica', 'bold');
    pdf.text('WhatsApp Us', whatsappBtnX + 25, contactY + 8, { align: 'center' });
    pdf.link(whatsappBtnX, contactY, whatsappBtnWidth, 12, { url: 'https://wa.me/27833269469' });

    ctaY += 65;

    // Website at bottom
    pdf.setFontSize(11);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(59, 130, 246);
    pdf.text('https://interon.co.za', pdfWidth / 2, ctaY, { align: 'center' });
    pdf.link(margin, ctaY - 5, contentWidth, 10, { url: 'https://interon.co.za' });

    // Add footer with logo
    addFooter();

    console.log(`PDF complete: ${currentPage} pages total`);

    const fileName = `ai-readiness-report-${new URL(currentResults.url).hostname}-${new Date().toISOString().split('T')[0]}.pdf`;
    console.log(`Saving as: ${fileName}`);
    pdf.save(fileName);

  } catch (error) {
    console.error('PDF export failed:', error);
    alert('Failed to export PDF. Please try again.');
  } finally {
    btn.disabled = false;
    btnText.textContent = 'Export PDF';
  }
}

// Allow Enter key to submit
document.addEventListener('DOMContentLoaded', function() {
  const input = document.getElementById('url-input');
  if (input) {
    input.addEventListener('keypress', function(e) {
      if (e.key === 'Enter') {
        runAudit();
      }
    });
  }
});
