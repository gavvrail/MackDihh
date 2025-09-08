// Profile Management JavaScript - Simplified Version
let cropper = null;
let currentFile = null;

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('Profile management script loaded');
    initializeProfilePictureUpload();
    initializeCropModal();
});

// Initialize profile picture upload functionality
function initializeProfilePictureUpload() {
    const profilePhoto = document.getElementById('profile-photo');
    const profilePhotoInput = document.getElementById('profile-photo-input');
    const profileImageContainer = document.querySelector('.profile-image-container');

    if (!profilePhoto || !profilePhotoInput || !profileImageContainer) {
        console.log('Profile picture elements not found');
        return;
    }

    // Make the profile image container clickable
    profileImageContainer.style.cursor = 'pointer';
    
    // Add click event to the container
    profileImageContainer.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        console.log('Profile image container clicked');
        profilePhotoInput.click();
    });
    
    // Also add click event to the profile photo itself
    profilePhoto.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        console.log('Profile photo clicked');
        profilePhotoInput.click();
    });
    
    // Add click event to the edit overlay
    const editOverlay = document.querySelector('.profile-edit-overlay');
    if (editOverlay) {
        editOverlay.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            console.log('Edit overlay clicked');
            profilePhotoInput.click();
        });
    }

    // Handle file selection
    profilePhotoInput.addEventListener('change', function(e) {
        const file = e.target.files[0];
        if (file) {
            console.log('File selected:', file.name, file.size);
            
            // Validate file type
            if (!file.type.startsWith('image/')) {
                alert('Please select a valid image file.');
                return;
            }

            // Validate file size (max 5MB)
            if (file.size > 5 * 1024 * 1024) {
                alert('Image size must be less than 5MB.');
                return;
            }

            // Ask user if they want to crop
            const shouldCrop = confirm('Would you like to crop your image before uploading?');
            if (shouldCrop) {
                showCropModal(file);
            } else {
                uploadProfilePicture(file);
            }
        }
    });
}

// Initialize crop modal functionality
function initializeCropModal() {
    const cropAndUploadBtn = document.getElementById('cropAndUploadBtn');
    const cropModal = document.getElementById('cropModal');

    if (cropAndUploadBtn) {
        cropAndUploadBtn.addEventListener('click', function(e) {
            e.preventDefault();
            console.log('Crop & Upload button clicked');
            
            if (cropper) {
                try {
                    const canvas = cropper.getCroppedCanvas({
                        width: 300,
                        height: 300,
                        minWidth: 256,
                        minHeight: 256,
                        maxWidth: 4096,
                        maxHeight: 4096,
                        fillColor: '#fff',
                        imageSmoothingEnabled: true,
                        imageSmoothingQuality: 'high',
                    });

                    if (canvas) {
                        const base64String = canvas.toDataURL('image/jpeg', 0.9);
                        console.log('Canvas created, uploading...');
                        
                        // Show preview immediately
                        const profilePhoto = document.getElementById('profile-photo');
                        if (profilePhoto) {
                            profilePhoto.src = base64String;
                        }
                        
                        // Upload the cropped image
                        uploadCroppedImage(base64String);
                        
                        // Close modal
                        const modal = bootstrap.Modal.getInstance(cropModal);
                        if (modal) {
                            modal.hide();
                        }
                    } else {
                        alert('Failed to crop image. Please try again.');
                    }
                } catch (error) {
                    console.error('Error during crop:', error);
                    alert('An error occurred while cropping. Please try again.');
                }
            } else {
                alert('Crop tool not ready. Please try again.');
            }
        });
    }

    // Clean up when modal is hidden
    if (cropModal) {
        cropModal.addEventListener('hidden.bs.modal', function() {
            console.log('Modal hidden, cleaning up cropper');
            if (cropper) {
                cropper.destroy();
                cropper = null;
            }
            currentFile = null;
        });
    }
}

// Show crop modal with image
function showCropModal(file) {
    console.log('Showing crop modal for file:', file.name);
    currentFile = file;
    
    const reader = new FileReader();
    reader.onload = function(e) {
        const cropImage = document.getElementById('cropImage');
        const cropModal = document.getElementById('cropModal');
        
        if (!cropImage || !cropModal) {
            console.error('Crop modal elements not found');
            return;
        }

        // Set image source
        cropImage.src = e.target.result;
        
        // Show modal
        const modal = new bootstrap.Modal(cropModal);
        modal.show();
        
        // Initialize cropper when modal is shown
        cropModal.addEventListener('shown.bs.modal', function() {
            console.log('Modal shown, initializing cropper');
            
            // Destroy existing cropper
            if (cropper) {
                cropper.destroy();
                cropper = null;
            }
            
            // Wait a bit for the image to load
            setTimeout(() => {
                try {
                    cropper = new Cropper(cropImage, {
                        aspectRatio: 1,
                        viewMode: 1,
                        dragMode: 'move',
                        autoCropArea: 0.8,
                        cropBoxMovable: true,
                        cropBoxResizable: true,
                        background: false,
                        responsive: true,
                        checkOrientation: false,
                        zoomable: true,
                        rotatable: false,
                        scalable: false
                    });
                    console.log('Cropper initialized successfully');
                } catch (error) {
                    console.error('Failed to initialize cropper:', error);
                    alert('Failed to load crop tool. Please try uploading directly.');
                    modal.hide();
                    uploadProfilePicture(currentFile);
                }
            }, 100);
        }, { once: true });
    };
    
    reader.readAsDataURL(file);
}

// Upload profile picture directly (without cropping)
function uploadProfilePicture(file) {
    console.log('Uploading profile picture directly');
    
    const formData = new FormData();
    formData.append('profilePicture', file);

    const profilePhoto = document.getElementById('profile-photo');
    const originalSrc = profilePhoto ? profilePhoto.src : '';

    // Show loading state
    if (profilePhoto) {
        profilePhoto.style.opacity = '0.7';
    }

    fetch('/Profile/UploadProfilePicture', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            if (profilePhoto) {
                profilePhoto.src = data.profilePhotoUrl;
                profilePhoto.style.opacity = '1';
            }
            showMessage('Profile picture updated successfully!', 'success');
        } else {
            if (profilePhoto) {
                profilePhoto.src = originalSrc;
                profilePhoto.style.opacity = '1';
            }
            showMessage(data.message || 'Failed to upload profile picture.', 'error');
        }
    })
    .catch(error => {
        console.error('Error uploading profile picture:', error);
        if (profilePhoto) {
            profilePhoto.src = originalSrc;
            profilePhoto.style.opacity = '1';
        }
        showMessage('An error occurred while uploading the profile picture.', 'error');
    });
}

// Upload cropped image via AJAX
function uploadCroppedImage(base64String) {
    console.log('Uploading cropped image');
    
    const profilePhoto = document.getElementById('profile-photo');
    const originalSrc = profilePhoto ? profilePhoto.src : '';
    
    // Show loading state
    if (profilePhoto) {
        profilePhoto.style.opacity = '0.7';
    }

    fetch('/Profile/UploadCroppedProfilePicture', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify({
            imageData: base64String
        })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            if (profilePhoto) {
                profilePhoto.src = data.profilePhotoUrl;
                profilePhoto.style.opacity = '1';
            }
            showMessage('Profile picture cropped and updated successfully!', 'success');
        } else {
            if (profilePhoto) {
                profilePhoto.src = originalSrc;
                profilePhoto.style.opacity = '1';
            }
            showMessage(data.message || 'Failed to upload cropped profile picture.', 'error');
        }
    })
    .catch(error => {
        console.error('Error uploading cropped profile picture:', error);
        if (profilePhoto) {
            profilePhoto.src = originalSrc;
            profilePhoto.style.opacity = '1';
        }
        showMessage('An error occurred while uploading the cropped profile picture.', 'error');
    });
}

// Show message to user
function showMessage(message, type) {
    console.log('Showing message:', message, type);
    
    // Create message element
    const messageDiv = document.createElement('div');
    messageDiv.className = `alert alert-${type === 'success' ? 'success' : 'danger'} alert-dismissible fade show`;
    messageDiv.style.position = 'fixed';
    messageDiv.style.top = '20px';
    messageDiv.style.right = '20px';
    messageDiv.style.zIndex = '9999';
    messageDiv.style.minWidth = '300px';
    
    messageDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    // Add to page
    document.body.appendChild(messageDiv);

    // Auto remove after 5 seconds
    setTimeout(() => {
        if (messageDiv.parentNode) {
            messageDiv.parentNode.removeChild(messageDiv);
        }
    }, 5000);
}
