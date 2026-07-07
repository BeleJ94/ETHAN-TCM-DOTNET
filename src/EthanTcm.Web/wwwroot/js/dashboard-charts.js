(() => {
  const dataNode = document.getElementById("dashboard-chart-data");
  if (!dataNode || !window.Chart) {
    return;
  }

  let dashboardCharts;
  try {
    dashboardCharts = JSON.parse(dataNode.textContent || "{}");
  } catch {
    return;
  }

  const palette = {
    danger: "#c24135",
    warning: "#c88719",
    info: "#2f76a8",
    success: "#1f8a5b",
    neutral: "#6b7c8f"
  };

  const mutedPalette = {
    danger: "rgba(194, 65, 53, .15)",
    warning: "rgba(200, 135, 25, .16)",
    info: "rgba(47, 118, 168, .16)",
    success: "rgba(31, 138, 91, .16)",
    neutral: "rgba(107, 124, 143, .16)"
  };

  const toSeries = (points) => Array.isArray(points) ? points : [];
  const labels = (points) => points.map((point) => point.label);
  const values = (points) => points.map((point) => Number(point.value || 0));
  const colors = (points) => points.map((point) => palette[point.tone] || palette.neutral);
  const softColors = (points) => points.map((point) => mutedPalette[point.tone] || mutedPalette.neutral);
  const keys = (points) => points.map((point) => point.key);

  const buildDetailUrl = (chartType, segmentKey) => {
    if (!dashboardCharts.detailBaseUrl || !segmentKey || segmentKey === "empty") {
      return null;
    }

    const url = new URL(dashboardCharts.detailBaseUrl, window.location.origin);
    url.searchParams.set("chart", chartType);
    url.searchParams.set("segmentKey", segmentKey);
    return url.toString();
  };

  const openDetails = (chartType, point) => {
    const url = buildDetailUrl(chartType, point?.key);
    if (!url) {
      return;
    }

    document.dispatchEvent(new CustomEvent("dashboard:open-details", {
      detail: {
        url,
        title: point.label
      }
    }));
  };

  const clickableOptions = (chartType, points) => ({
    onClick(event, elements) {
      if (elements.length === 0) {
        return;
      }

      openDetails(chartType, points[elements[0].index]);
    },
    onHover(event, elements) {
      event.native.target.style.cursor = elements.length > 0 ? "pointer" : "default";
    }
  });

  const commonOptions = {
    responsive: true,
    maintainAspectRatio: false,
    animation: {
      duration: 650,
      easing: "easeOutQuart"
    },
    plugins: {
      legend: {
        display: false
      },
      tooltip: {
        backgroundColor: "rgba(18, 29, 43, .94)",
        padding: 10,
        displayColors: true,
        callbacks: {
          label(context) {
            const label = context.label || context.dataset.label || "";
            const value = context.parsed?.y ?? context.parsed?.x ?? context.parsed ?? 0;
            return ` ${label}: ${value}`;
          }
        }
      }
    }
  };

  const centerTextPlugin = {
    id: "dashboardCenterText",
    afterDraw(chart) {
      const { ctx, chartArea } = chart;
      const dataset = chart.data.datasets[0];
      if (!chartArea || !dataset) {
        return;
      }

      const total = dataset.data.reduce((sum, value) => sum + Number(value || 0), 0);
      const x = (chartArea.left + chartArea.right) / 2;
      const y = (chartArea.top + chartArea.bottom) / 2;
      ctx.save();
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      ctx.fillStyle = "#17202b";
      ctx.font = "700 20px system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif";
      ctx.fillText(String(total), x, y - 4);
      ctx.fillStyle = "#66758a";
      ctx.font = "700 10px system-ui, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif";
      ctx.fillText("total", x, y + 14);
      ctx.restore();
    }
  };

  Chart.register(centerTextPlugin);

  const renderDueDateChart = () => {
    const canvas = document.getElementById("dueDateChart");
    const points = toSeries(dashboardCharts.dueDateBuckets);
    if (!canvas || points.length === 0) {
      return;
    }

    new Chart(canvas, {
      type: "bar",
      data: {
        labels: labels(points),
        datasets: [{
          label: "Declarations",
          data: values(points),
          segmentKeys: keys(points),
          backgroundColor: colors(points),
          borderColor: colors(points),
          borderWidth: 1,
          borderRadius: 5,
          maxBarThickness: 42
        }]
      },
      options: {
        ...commonOptions,
        ...clickableOptions("DueDateBuckets", points),
        scales: {
          x: {
            grid: {
              display: false
            },
            ticks: {
              color: "#4a596b",
              font: {
                size: 11,
                weight: 700
              },
              maxRotation: 0
            }
          },
          y: {
            beginAtZero: true,
            ticks: {
              precision: 0,
              color: "#66758a"
            },
            grid: {
              color: "rgba(107, 124, 143, .16)"
            }
          }
        }
      }
    });
  };

  const renderDoughnutChart = (canvasId, source) => {
    const canvas = document.getElementById(canvasId);
    const points = toSeries(source);
    if (!canvas || points.length === 0) {
      return;
    }

    new Chart(canvas, {
      type: "doughnut",
      data: {
        labels: labels(points),
        datasets: [{
          data: values(points),
          segmentKeys: keys(points),
          backgroundColor: colors(points),
          borderColor: "#fff",
          borderWidth: 3,
          hoverOffset: 4
        }]
      },
      options: {
        ...commonOptions,
        ...clickableOptions(canvasId === "statusChart" ? "StatusDistribution" : "RiskDistribution", points),
        cutout: "68%"
      }
    });
  };

  const renderOwnerChart = () => {
    const canvas = document.getElementById("ownerChart");
    const points = toSeries(dashboardCharts.ownerWorkload);
    if (!canvas || points.length === 0) {
      return;
    }

    new Chart(canvas, {
      type: "bar",
      data: {
        labels: labels(points),
        datasets: [{
          label: "Dossiers ouverts",
          data: values(points),
          segmentKeys: keys(points),
          backgroundColor: softColors(points),
          borderColor: colors(points),
          borderWidth: 1,
          borderRadius: 5,
          maxBarThickness: 24
        }]
      },
      options: {
        ...commonOptions,
        ...clickableOptions("OwnerWorkload", points),
        indexAxis: "y",
        scales: {
          x: {
            beginAtZero: true,
            ticks: {
              precision: 0,
              color: "#66758a"
            },
            grid: {
              color: "rgba(107, 124, 143, .16)"
            }
          },
          y: {
            grid: {
              display: false
            },
            ticks: {
              color: "#4a596b",
              font: {
                size: 11,
                weight: 700
              }
            }
          }
        }
      }
    });
  };

  renderDueDateChart();
  renderDoughnutChart("statusChart", dashboardCharts.statusDistribution);
  renderDoughnutChart("riskChart", dashboardCharts.riskDistribution);
  renderOwnerChart();
})();
