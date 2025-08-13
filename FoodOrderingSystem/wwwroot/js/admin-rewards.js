// Admin Points Rewards Management JavaScript

// Function to confirm and trigger delete
function confirmDelete(id, title) {
    document.getElementById('deleteId').value = id;
    document.getElementById('rewardTitle').textContent = title;
    var deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
    deleteModal.show();
}

// Auto-hide success alerts after 5 seconds
document.addEventListener('DOMContentLoaded', function() {
    setTimeout(function() {
        const successAlerts = document.querySelectorAll('.alert-success');
        successAlerts.forEach(function(alert) {
            alert.style.transition = 'opacity 0.5s ease';
            alert.style.opacity = '0';
            setTimeout(function() {
                alert.remove();
            }, 500);
        });
    }, 5000);
});
