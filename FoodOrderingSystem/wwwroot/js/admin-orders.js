// Admin Orders Management JavaScript

// Function to update order status
function updateOrderStatus(orderId, status) {
    if (confirm(`Are you sure you want to mark this order as ${status}?`)) {
        // Get the URLs and token from the global variables
        const updateUrl = window.adminOrdersConfig?.updateUrl || '/Admin/UpdateOrderStatus';
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        
        fetch(updateUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: `orderId=${orderId}&status=${status}`
        })
        .then(response => {
            if (response.ok) {
                location.reload();
            } else {
                alert('Error updating order status');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('An error occurred while updating the order status.');
        });
    }
}

// Function to view order details
function viewOrderDetails(orderId) {
    console.log('Viewing order details for ID:', orderId);
    
    // Get the URL from the global variables
    const getDetailsUrl = window.adminOrdersConfig?.getDetailsUrl || '/Admin/GetOrderDetails';
    
    // Fetch order details via AJAX
    fetch(`${getDetailsUrl}/${orderId}`)
        .then(response => {
            console.log('Response status:', response.status);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            console.log('Response data:', data);
            if (data.success) {
                const order = data.order;
                document.getElementById('orderDetailsContent').innerHTML = `
                    <div class="row">
                        <div class="col-md-6">
                            <h6><i class="fas fa-receipt me-2"></i>Order Information</h6>
                            <p><strong>Order Number:</strong> ${order.orderNumber}</p>
                            <p><strong>Customer:</strong> ${order.customerName}</p>
                            <p><strong>Order Date:</strong> ${new Date(order.orderDate).toLocaleDateString()}</p>
                            <p><strong>Status:</strong> <span class="badge bg-primary">${order.status}</span></p>
                            <p><strong>Total:</strong> RM${order.total.toFixed(2)}</p>
                        </div>
                        <div class="col-md-6">
                            <h6><i class="fas fa-map-marker-alt me-2"></i>Delivery Details</h6>
                            <p><strong>Address:</strong> ${order.deliveryAddress || 'N/A'}</p>
                            <p><strong>Phone:</strong> ${order.customerPhone || 'N/A'}</p>
                            <p><strong>Instructions:</strong> ${order.deliveryInstructions || 'None'}</p>
                            <p><strong>Estimated Delivery:</strong> ${order.estimatedDeliveryTime ? new Date(order.estimatedDeliveryTime).toLocaleString() : 'N/A'}</p>
                        </div>
                    </div>
                    <hr>
                    <h6><i class="fas fa-utensils me-2"></i>Order Items</h6>
                    <div class="table-responsive">
                        <table class="table table-sm">
                            <thead>
                                <tr>
                                    <th>Item</th>
                                    <th>Quantity</th>
                                    <th>Price</th>
                                    <th>Total</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${order.orderItems.map(item => `
                                    <tr>
                                        <td>${item.menuItemName}</td>
                                        <td>${item.quantity}</td>
                                        <td>RM${item.price.toFixed(2)}</td>
                                        <td>RM${(item.quantity * item.price).toFixed(2)}</td>
                                    </tr>
                                `).join('')}
                            </tbody>
                        </table>
                    </div>
                    ${order.notes ? `
                        <hr>
                        <h6><i class="fas fa-sticky-note me-2"></i>Order Notes</h6>
                        <p class="text-muted">${order.notes}</p>
                    ` : ''}
                    <hr>
                    <div class="text-center">
                        <h6>Quick Status Update</h6>
                        <div class="btn-group" role="group">
                            <button type="button" class="btn btn-sm btn-outline-info" onclick="updateOrderStatus(${orderId}, 'Confirmed')">Confirm</button>
                            <button type="button" class="btn btn-sm btn-outline-warning" onclick="updateOrderStatus(${orderId}, 'Preparing')">Preparing</button>
                            <button type="button" class="btn btn-sm btn-outline-primary" onclick="updateOrderStatus(${orderId}, 'Delivering')">Delivering</button>
                            <button type="button" class="btn btn-sm btn-outline-success" onclick="updateOrderStatus(${orderId}, 'Delivered')">Delivered</button>
                        </div>
                    </div>
                `;
            } else {
                document.getElementById('orderDetailsContent').innerHTML = `
                    <div class="alert alert-danger">
                        <i class="fas fa-exclamation-triangle me-2"></i>
                        Error loading order details: ${data.message}
                    </div>
                `;
            }
        })
        .catch(error => {
            console.error('Error:', error);
            document.getElementById('orderDetailsContent').innerHTML = `
                <div class="alert alert-danger">
                    <i class="fas fa-exclamation-triangle me-2"></i>
                    An error occurred while loading order details.
                </div>
            `;
        });
    
    var modal = new bootstrap.Modal(document.getElementById('orderDetailsModal'));
    modal.show();
}
