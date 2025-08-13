// Admin Reports Charts JavaScript
document.addEventListener('DOMContentLoaded', function() {
    // Get data passed from the view
    const reportData = window.reportData || {};
    
    // Monthly Sales Chart
    const monthlySalesCtx = document.getElementById('monthlySalesChart');
    if (monthlySalesCtx && reportData.monthlySales) {
        new Chart(monthlySalesCtx.getContext('2d'), {
            type: 'line',
            data: {
                labels: reportData.monthlySales.labels,
                datasets: [{
                    label: 'Sales (RM)',
                    data: reportData.monthlySales.data,
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return 'RM' + value.toFixed(2);
                            }
                        }
                    }
                }
            }
        });
    }

    // Order Status Pie Chart
    const orderStatusCtx = document.getElementById('orderStatusChart');
    if (orderStatusCtx && reportData.orderStatus) {
        new Chart(orderStatusCtx.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: reportData.orderStatus.labels,
                datasets: [{
                    data: reportData.orderStatus.data,
                    backgroundColor: [
                        '#FF6384',
                        '#36A2EB',
                        '#FFCE56',
                        '#4BC0C0',
                        '#9966FF',
                        '#FF9F40'
                    ],
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 20,
                            usePointStyle: true
                        }
                    }
                }
            }
        });
    }

    // Top Categories Bar Chart
    const topCategoriesCtx = document.getElementById('topCategoriesChart');
    if (topCategoriesCtx && reportData.topCategories) {
        new Chart(topCategoriesCtx.getContext('2d'), {
            type: 'bar',
            data: {
                labels: reportData.topCategories.labels,
                datasets: [{
                    label: 'Sales (RM)',
                    data: reportData.topCategories.data,
                    backgroundColor: [
                        'rgba(255, 99, 132, 0.8)',
                        'rgba(54, 162, 235, 0.8)',
                        'rgba(255, 205, 86, 0.8)',
                        'rgba(75, 192, 192, 0.8)',
                        'rgba(153, 102, 255, 0.8)'
                    ],
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 205, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)'
                    ],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return 'RM' + value.toFixed(2);
                            }
                        }
                    }
                }
            }
        });
    }

    // Daily Orders Chart (Sample data for last 7 days)
    const dailyOrdersCtx = document.getElementById('dailyOrdersChart');
    if (dailyOrdersCtx) {
        const last7Days = [];
        const orderCounts = [];
        
        for (let i = 6; i >= 0; i--) {
            const date = new Date();
            date.setDate(date.getDate() - i);
            last7Days.push(date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' }));
            // Generate sample data - in a real app, this would come from the controller
            orderCounts.push(Math.floor(Math.random() * 10) + 1);
        }

        new Chart(dailyOrdersCtx.getContext('2d'), {
            type: 'line',
            data: {
                labels: last7Days,
                datasets: [{
                    label: 'Orders',
                    data: orderCounts,
                    borderColor: 'rgb(153, 102, 255)',
                    backgroundColor: 'rgba(153, 102, 255, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });
    }
});
