import "./styles.css";

const project = {
  "slug": "ArchiveFlow",
  "title": "ArchiveFlow Studio",
  "repoUrl": "https://github.com/Justin21523/archive-flow-studio",
  "readmeUrl": "https://github.com/Justin21523/archive-flow-studio#readme",
  "domain": "ai-workflow",
  "summary": "ArchiveFlow Studio is a cross-platform desktop app built with C#, .NET 9, and Avalonia. Its core is a node-based metadata workflow canvas backed by SQLite, Dapper, FluentMigrator, ImageSharp, FTS5 full-text search, background jobs, and a plugin-oriented architecture.",
  "problem": "Personal and small-organization digital files often live in scattered folders without consistent metadata, relationship context, batch processing workflows, or exchangeable archive formats.",
  "solution": "I designed a node-based workflow system where users can connect file sources, filters, metadata actions, relationship builders, and output nodes into repeatable processing pipelines. SQLite and FTS5 provide local persistence and search.",
  "architecture": "This case study is generated from the portfolio catalog pipeline using README, Git metadata, package/build configuration, and media signals. The final architecture narrative still needs source-level review. Current detected technology signals include: C#, .NET, Avalonia, SQLite, Dapper, FluentMigrator, ImageSharp, MVVM, Clean Architecture.",
  "features": [
    "Detected technical signals: C#,",
    "NET, Avalonia, SQLite, Dapper, FluentMigrator, ImageSharp, MVVM, Clean Architecture,README evidence exists and can support a fuller reviewed case study,A public GitHub repository is linked for source traceability",
    "🖼️ System Showcase",
    "🏆 Portfolio Significance & Outcomes",
    "Metadata & Standards",
    "Professional Features",
    "Clean Architecture Layers",
    "I designed a node-based workflow system where users can connect file sources, filters, metadata actions, relationship builders, and output nodes into repeatable processing pipelines"
  ],
  "modules": [
    ".gitignore",
    "ArchiveFlow.sln",
    "Data/",
    "README.md",
    "data/",
    "plugins/",
    "src/",
    "tests/"
  ],
  "headings": [
    "ArchiveFlow Studio",
    "🖼️ System Showcase",
    "🏆 Portfolio Significance & Outcomes",
    "Features",
    "Core Functionality",
    "File Management",
    "Metadata & Standards",
    "Professional Features",
    "️ Architecture",
    "Clean Architecture Layers"
  ],
  "sampleRows": [
    {
      "id": "S01",
      "label": "Detected technical signals: C#,",
      "value": 42,
      "delta": "+12%",
      "status": "ready"
    },
    {
      "id": "S02",
      "label": "NET, Avalonia, SQLite, Dapper, FluentMigrator, ImageSharp, MVVM, Clean Architecture,README evidence exists and can support a fuller reviewed case study,A public GitHub repository is linked for source traceability",
      "value": 51,
      "delta": "+7%",
      "status": "review"
    },
    {
      "id": "S03",
      "label": "🖼️ System Showcase",
      "value": 60,
      "delta": "+12%",
      "status": "complete"
    },
    {
      "id": "S04",
      "label": "🏆 Portfolio Significance & Outcomes",
      "value": 69,
      "delta": "+7%",
      "status": "ready"
    },
    {
      "id": "S05",
      "label": "Metadata & Standards",
      "value": 78,
      "delta": "+12%",
      "status": "review"
    },
    {
      "id": "S06",
      "label": "Professional Features",
      "value": 87,
      "delta": "+7%",
      "status": "complete"
    }
  ],
  "scenarios": [
    {
      "id": "01",
      "name": "Input assets",
      "status": "Ready",
      "detail": "Detected technical signals: C#,"
    },
    {
      "id": "02",
      "name": "Preprocessing",
      "status": "Ready",
      "detail": "NET, Avalonia, SQLite, Dapper, FluentMigrator, ImageSharp, MVVM, Clean Architecture,README evidence exists and can support a fuller reviewed case study,A public GitHub repository is linked for source traceability"
    },
    {
      "id": "03",
      "name": "Model pass",
      "status": "Ready",
      "detail": "🖼️ System Showcase"
    },
    {
      "id": "04",
      "name": "Evaluation",
      "status": "Review",
      "detail": "🏆 Portfolio Significance & Outcomes"
    },
    {
      "id": "05",
      "name": "Export",
      "status": "Review",
      "detail": "Metadata & Standards"
    }
  ]
};
let view = "workspace";
let selected = project.scenarios[0]?.id ?? "01";

function metric(label, value) {
  return `<div class="metric"><span>${label}</span><strong>${value}</strong></div>`;
}

function nav() {
  return ["workspace", "workflow", "visualization", "evidence", "architecture"].map((item) =>
    `<button class="${view === item ? "active" : ""}" data-view="${item}">${item}</button>`
  ).join("");
}

function workspace() {
  return `
    <section class="hero">
      <div>
        <p class="eyebrow">${project.domain.replaceAll("-", " ")}</p>
        <h1>${project.title}</h1>
        <p class="lead">${project.summary}</p>
      </div>
      <div class="metrics">
        ${metric("Workflow steps", project.scenarios.length)}
        ${metric("Source modules", project.modules.length)}
        ${metric("Review mode", "Static")}
        ${metric("Backend", "Fixture")}
      </div>
    </section>
    <section class="split">
      <article><h2>Problem</h2><p>${project.problem}</p></article>
      <article><h2>Implemented Result</h2><p>${project.solution}</p></article>
    </section>
  `;
}

function workflow() {
  return `
    <section>
      <div class="section-head">
        <div><p class="eyebrow">project workflow</p><h2>Executable Review Path</h2></div>
        <button class="primary" id="run">Run workflow</button>
      </div>
      <div class="board">
        ${project.scenarios.map((item) => `
          <button class="card ${selected === item.id ? "selected" : ""}" data-step="${item.id}">
            <span>${item.id}</span>
            <strong>${item.name}</strong>
            <em>${item.status}</em>
            <p>${item.detail}</p>
          </button>
        `).join("")}
      </div>
      <output id="output">Select a step or run the workflow to inspect deterministic project output.</output>
    </section>
  `;
}

function visualization() {
  const max = Math.max(...project.sampleRows.map((row) => row.value), 1);
  return `
    <section>
      <div class="section-head">
        <div><p class="eyebrow">sample data result</p><h2>Visible Demo Output</h2></div>
        <span class="badge">Fixture-backed</span>
      </div>
      <div class="viz">
        <div class="bars">
          ${project.sampleRows.map((row) => `
            <div class="bar-row">
              <span>${row.id}</span>
              <div class="bar-track"><div class="bar-fill" style="width: ${Math.round(row.value / max * 100)}%"></div></div>
              <strong>${row.value}</strong>
            </div>
          `).join("")}
        </div>
        <div class="result-table">
          ${project.sampleRows.map((row) => `
            <article>
              <b>${row.label}</b>
              <span>${row.status}</span>
              <em>${row.delta}</em>
            </article>
          `).join("")}
        </div>
      </div>
    </section>
  `;
}

function evidence() {
  return `
    <section class="split">
      <article>
        <p class="eyebrow">repository evidence</p>
        <h2>Source modules</h2>
        <div class="chips">${project.modules.map((item) => `<span>${item}</span>`).join("") || "<span>Project source reviewed</span>"}</div>
      </article>
      <article>
        <p class="eyebrow">documentation</p>
        <h2>README signals</h2>
        <ul>${project.headings.map((item) => `<li>${item}</li>`).join("") || "<li>README content is represented in the workflow panels.</li>"}</ul>
      </article>
    </section>
  `;
}

function architecture() {
  return `
    <section>
      <p class="eyebrow">architecture</p>
      <h2>Static deployment architecture</h2>
      <p>${project.architecture}</p>
      <pre>npm run dev
npm run build
GitHub Pages / gh-pages
local fixtures / no private backend</pre>
    </section>
  `;
}

function render() {
  const views = { workspace, workflow, visualization, evidence, architecture };
  document.querySelector("#app").innerHTML = `
    <header>
      <a href="${project.repoUrl}" class="brand">${project.title}</a>
      <nav>${nav()}</nav>
      <a class="readme" href="${project.readmeUrl}">README</a>
    </header>
    <main>${views[view]()}</main>
  `;
  document.querySelectorAll("[data-view]").forEach((button) => button.addEventListener("click", () => {
    view = button.dataset.view;
    render();
  }));
  document.querySelectorAll("[data-step]").forEach((button) => button.addEventListener("click", () => {
    selected = button.dataset.step;
    render();
  }));
  document.querySelector("#run")?.addEventListener("click", () => {
    const output = document.querySelector("#output");
    if (output) output.textContent = `${project.title}: ${project.scenarios.length} workflow steps completed using local fixture state.`;
  });
}

render();
