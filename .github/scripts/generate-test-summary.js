/**
 * Parses TRX test results and Cobertura coverage reports and outputs
 * formatted GitHub Flavored Markdown to stdout or GITHUB_STEP_SUMMARY.
 */
const fs = require('fs');
const path = require('path');

function findFiles(dir, filter, fileList = []) {
  if (!fs.existsSync(dir)) return fileList;
  for (const file of fs.readdirSync(dir)) {
    const fullPath = path.join(dir, file);
    try {
      const stat = fs.statSync(fullPath);
      if (stat.isDirectory()) {
        findFiles(fullPath, filter, fileList);
      } else if (filter.test(file)) {
        fileList.push(fullPath);
      }
    } catch (_) {
      // Ignore unreadable paths
    }
  }
  return fileList;
}

function parseDuration(startStr, finishStr) {
  try {
    const cleanStart = startStr.replace(/(\.\d{3})\d+/, '$1');
    const cleanFinish = finishStr.replace(/(\.\d{3})\d+/, '$1');
    const ms = new Date(cleanFinish) - new Date(cleanStart);
    if (!isNaN(ms) && ms >= 0) {
      return (ms / 1000).toFixed(2) + 's';
    }
  } catch (_) {}
  return 'N/A';
}

function generateSummary() {
  const testResultsDir = path.resolve(process.argv[2] || 'tests/IdentityService.UnitTests/TestResults');

  const trxFiles = findFiles(testResultsDir, /\.trx$/);
  const covFiles = findFiles(testResultsDir, /coverage\.cobertura\.xml$/);

  let summary = '# 🧪 Unit Test & Code Coverage Summary\n\n';

  // 1. Process TRX Test Results
  if (trxFiles.length > 0) {
    try {
      const trxContent = fs.readFileSync(trxFiles[0], 'utf8');
      const countersMatch = trxContent.match(/<Counters\s+([^>]+)\/>/);
      const timesMatch = trxContent.match(/<Times\s+[^>]*start="([^"]+)"[^>]*finish="([^"]+)"/);

      if (countersMatch) {
        const attrs = {};
        countersMatch[1].replace(/(\w+)="([^"]+)"/g, (_, k, v) => { attrs[k] = v; });

        const total = parseInt(attrs.total || '0', 10);
        const passed = parseInt(attrs.passed || '0', 10);
        const failed = parseInt(attrs.failed || '0', 10);
        const skipped = parseInt(attrs.notExecuted || attrs.skipped || '0', 10);
        const duration = timesMatch ? parseDuration(timesMatch[1], timesMatch[2]) : 'N/A';

        const statusBadge = failed === 0 ? '🟢 All Passed' : '🔴 Tests Failed';
        summary += `### 📊 Test Results: ${statusBadge}\n\n`;
        summary += '| Total Tests | Passed | Failed | Skipped | Duration |\n';
        summary += '| :---: | :---: | :---: | :---: | :---: |\n';
        summary += `| **${total}** | **${passed}** ✅ | **${failed}** ${failed > 0 ? '❌' : ''} | **${skipped}** | **${duration}** |\n\n`;

        // Extract failed tests details if any
        if (failed > 0) {
          const failedTestRegex = /<UnitTestResult\s+[^>]*testName="([^"]+)"[^>]*outcome="Failed"[^>]*>([\s\S]*?)<\/UnitTestResult>/g;
          let failMatch;
          const failedDetails = [];
          while ((failMatch = failedTestRegex.exec(trxContent)) !== null) {
            const name = failMatch[1];
            const msgMatch = failMatch[2].match(/<Message>([\s\S]*?)<\/Message>/);
            const msg = msgMatch ? msgMatch[1].trim().replace(/\r?\n/g, ' ') : 'Failed without message';
            failedDetails.push({ name, msg });
          }

          if (failedDetails.length > 0) {
            summary += '<details open>\n<summary><b>❌ Failed Tests Details</b></summary>\n\n';
            summary += '| Test | Error Message |\n';
            summary += '| :--- | :--- |\n';
            for (const item of failedDetails) {
              summary += `| \`${item.name}\` | ${item.msg.slice(0, 200)} |\n`;
            }
            summary += '\n</details>\n\n';
          }
        }
      }
    } catch (err) {
      summary += `> ⚠️ *Could not parse test results: ${err.message}*\n\n`;
    }
  } else {
    summary += '> ℹ️ *No .trx test result files found.*\n\n';
  }

  // 2. Process Cobertura Coverage
  if (covFiles.length > 0) {
    try {
      const covContent = fs.readFileSync(covFiles[0], 'utf8');
      const lineRateMatch = covContent.match(/line-rate="([^"]+)"/);
      const branchRateMatch = covContent.match(/branch-rate="([^"]+)"/);
      const linesCoveredMatch = covContent.match(/lines-covered="([^"]+)"/);
      const linesValidMatch = covContent.match(/lines-valid="([^"]+)"/);

      if (lineRateMatch && linesCoveredMatch) {
        const lineRate = (parseFloat(lineRateMatch[1]) * 100).toFixed(1);
        const branchRate = (parseFloat(branchRateMatch[1]) * 100).toFixed(1);
        const lineBadge = lineRate >= 80 ? '🟢' : lineRate >= 60 ? '🟡' : '🔴';
        const branchBadge = branchRate >= 80 ? '🟢' : branchRate >= 60 ? '🟡' : '🔴';

        summary += '### 📈 Code Coverage\n\n';
        summary += '| Metric | Coverage | Status | Details |\n';
        summary += '| :--- | :---: | :---: | :--- |\n';
        summary += `| **Line Coverage** | **${lineRate}%** | ${lineBadge} | ${linesCoveredMatch[1]} of ${linesValidMatch[1]} lines covered |\n`;
        summary += `| **Branch Coverage** | **${branchRate}%** | ${branchBadge} | Decision branch coverage |\n\n`;

        // Package breakdown
        const packageRegex = /<package\s+name="([^"]+)"\s+line-rate="([^"]+)"\s+branch-rate="([^"]+)"/g;
        let pkgMatch;
        const packages = [];
        while ((pkgMatch = packageRegex.exec(covContent)) !== null) {
          packages.push({
            name: pkgMatch[1],
            lineRate: (parseFloat(pkgMatch[2]) * 100).toFixed(1),
            branchRate: (parseFloat(pkgMatch[3]) * 100).toFixed(1)
          });
        }

        if (packages.length > 0) {
          summary += '<details open>\n<summary><b>📦 Coverage by Project / Package</b></summary>\n\n';
          summary += '| Package | Line Coverage | Branch Coverage |\n';
          summary += '| :--- | :---: | :---: |\n';
          for (const pkg of packages) {
            const pkgBadge = pkg.lineRate >= 80 ? '🟢' : pkg.lineRate >= 60 ? '🟡' : '🔴';
            summary += `| \`${pkg.name}\` | ${pkgBadge} ${pkg.lineRate}% | ${pkg.branchRate}% |\n`;
          }
          summary += '\n</details>\n';
        }
      }
    } catch (err) {
      summary += `> ⚠️ *Could not parse coverage data: ${err.message}*\n\n`;
    }
  } else {
    summary += '> ℹ️ *No coverage.cobertura.xml report found.*\n';
  }

  return summary;
}

const output = generateSummary();

// If GITHUB_STEP_SUMMARY environment variable is present, append to it
if (process.env.GITHUB_STEP_SUMMARY) {
  try {
    fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, output + '\n');
  } catch (err) {
    console.error('Failed to write to GITHUB_STEP_SUMMARY:', err);
  }
}

// Always print to console
console.log(output);
