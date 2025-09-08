// Image Upload and Cropping functionality
class ImageUploader {
    constructor(options = {}) {
        this.options = {
            uploadUrl: options.uploadUrl || '/Profile/UploadProfilePicture',
            croppedUploadUrl: options.croppedUploadUrl || '/Profile/UploadCroppedProfilePicture',
            maxFileSize: options.maxFileSize || 5 * 1024 * 1024, // 5MB
            allowedTypes: options.allowedTypes || ['image/jpeg', 'image/png', 'image/gif'],
            cropAspectRatio: options.cropAspectRatio || 1,
            cropWidth: options.cropWidth || 200,
            cropHeight: options.cropHeight || 200,
            hideUploadArea: options.hideUploadArea || false,
            ...options
        };

        this.currentFile = null;
        this.cropper = null;
        this.uploadId = 'upload_' + Math.random().toString(36).substring(2, 11);
        this.init();
    }

    init() {
        if (!this.options.hideUploadArea) {
            this.createUploadArea();
        } else {
            // Remove any existing upload areas when hideUploadArea is true
            this.removeExistingUploadAreas();
        }
        this.bindEvents();
    }

    createUploadArea() {
        // Check if upload area already exists
        const targetElement = document.querySelector(this.options.target || '.profile-image-section');
        if (!targetElement) {
            console.error('Target element not found for image uploader');
            return;
        }

        // Check if upload area already exists
        const existingUploadArea = targetElement.querySelector('.image-upload-area');
        if (existingUploadArea) {
            console.log('Upload area already exists, skipping creation');
            return;
        }

        const uploadArea = document.createElement('div');
        uploadArea.className = 'image-upload-area';
        uploadArea.innerHTML = `
            <div class="upload-zone" id="uploadZone_${this.uploadId}">
                <div class="upload-content">
                    <i class="fas fa-cloud-upload-alt fa-3x text-muted mb-3"></i>
                    <h5>Drag & Drop Image Here</h5>
                    <p class="text-muted">or click to browse</p>
                    <input type="file" id="fileInput_${this.uploadId}" accept="image/*" style="display: none;">
                </div>
            </div>
            <div class="crop-container" id="cropContainer_${this.uploadId}" style="display: none;">
                <div class="crop-preview">
                    <img id="cropImage_${this.uploadId}" src="" alt="Crop Preview">
                </div>
                <div class="crop-controls">
                    <button type="button" class="btn btn-primary" id="cropBtn_${this.uploadId}">Crop & Save</button>
                    <button type="button" class="btn btn-secondary" id="cancelCropBtn_${this.uploadId}">Cancel</button>
                </div>
            </div>
        `;

        // Insert the upload area into the page
        targetElement.appendChild(uploadArea);
    }

    removeExistingUploadAreas() {
        const targetElement = document.querySelector(this.options.target || '.profile-image-section');
        if (targetElement) {
            const existingUploadAreas = targetElement.querySelectorAll('.image-upload-area');
            existingUploadAreas.forEach(area => area.remove());
        }
    }

    bindEvents() {
        if (!this.options.hideUploadArea) {
            this.bindUploadAreaEvents();
        }
        this.bindProfileImageEvents();

        // Bind crop interface events for profile images
        if (this.options.hideUploadArea) {
            this.bindCropInterfaceEvents();
        }
    }

    bindUploadAreaEvents() {
        const uploadZone = document.getElementById(`uploadZone_${this.uploadId}`);
        const fileInput = document.getElementById(`fileInput_${this.uploadId}`);
        const cropBtn = document.getElementById(`cropBtn_${this.uploadId}`);
        const cancelCropBtn = document.getElementById(`cancelCropBtn_${this.uploadId}`);

        if (!uploadZone || !fileInput) return;

        // Drag and drop events
        uploadZone.addEventListener('dragover', (e) => {
            e.preventDefault();
            uploadZone.classList.add('dragover');
        });

        uploadZone.addEventListener('dragleave', (e) => {
            e.preventDefault();
            uploadZone.classList.remove('dragover');
        });

        uploadZone.addEventListener('drop', (e) => {
            e.preventDefault();
            uploadZone.classList.remove('dragover');
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                this.handleFile(files[0]);
            }
        });

        // Click to browse
        uploadZone.addEventListener('click', () => {
            fileInput.click();
        });

        fileInput.addEventListener('change', (e) => {
            if (e.target.files.length > 0) {
                this.handleFile(e.target.files[0]);
            }
        });

        // Crop controls
        if (cropBtn) {
            cropBtn.addEventListener('click', () => {
                this.cropAndSave();
            });
        }

        if (cancelCropBtn) {
            cancelCropBtn.addEventListener('click', () => {
                this.cancelCrop();
            });
        }
    }

    bindProfileImageEvents() {
        const profileImageContainer = document.querySelector('.profile-image-container');
        const profilePhotoInput = document.getElementById('profile-photo-input');

        if (profileImageContainer && profilePhotoInput) {
            // Check if event listeners are already attached to prevent duplicates
            if (!profileImageContainer.dataset.uploaderInitialized) {
                // Click on profile image container to trigger file input
                profileImageContainer.addEventListener('click', () => {
                    profilePhotoInput.click();
                });

                // Handle file selection
                profilePhotoInput.addEventListener('change', (e) => {
                    if (e.target.files.length > 0) {
                        this.handleFile(e.target.files[0]);
                    }
                });

                // Mark as initialized to prevent duplicate event listeners
                profileImageContainer.dataset.uploaderInitialized = 'true';
            }
        }
    }

    bindCropInterfaceEvents() {
        const saveCropBtn = document.getElementById('saveCropBtn');
        const cancelCropBtn = document.getElementById('cancelCropBtn');

        if (saveCropBtn) {
            saveCropBtn.addEventListener('click', () => {
                this.cropAndSave();
            });
        }

        if (cancelCropBtn) {
            cancelCropBtn.addEventListener('click', () => {
                this.cancelCrop();
            });
        }
    }

    handleFile(file) {
        console.log('Handling file:', file);

        // Validate file
        if (!this.validateFile(file)) {
            return;
        }

        this.currentFile = file;
        console.log('File validated, showing crop interface');
        this.showCropInterface();
    }

    validateFile(file) {
        // Check file type
        if (!this.options.allowedTypes.includes(file.type)) {
            this.showError('Please select a valid image file (JPG, PNG, or GIF)');
            return false;
        }

        // Check file size
        if (file.size > this.options.maxFileSize) {
            this.showError(`File size must be less than ${this.options.maxFileSize / (1024 * 1024)}MB`);
            return false;
        }

        return true;
    }

    showCropInterface() {
        // For profile images, use the dedicated crop interface
        if (this.options.hideUploadArea) {
            const cropInterface = document.getElementById('cropInterface');
            const cropImage = document.getElementById('cropImage');
            const profileImageContainer = document.querySelector('.profile-image-container');

            if (cropInterface && cropImage && profileImageContainer) {
                // Hide profile image container and show crop interface
                profileImageContainer.style.display = 'none';
                cropInterface.style.display = 'block';

                // Load image for cropping
                const reader = new FileReader();
                reader.onload = (e) => {
                    cropImage.src = e.target.result;
                    // FIX: Wait for the image element to fully load before initializing the cropper.
                    // This prevents the browser from freezing.
                    cropImage.onload = () => {
                        this.initCropper();
                    };
                };
                reader.readAsDataURL(this.currentFile);
            } else {
                console.log('Profile crop interface not found, uploading original image');
                this.uploadOriginalImage();
            }
        } else {
            // For regular upload areas
            const uploadZone = document.getElementById(`uploadZone_${this.uploadId}`);
            const cropContainer = document.getElementById(`cropContainer_${this.uploadId}`);
            const cropImage = document.getElementById(`cropImage_${this.uploadId}`);

            if (uploadZone && cropContainer && cropImage) {
                // Hide upload zone and show crop container
                uploadZone.style.display = 'none';
                cropContainer.style.display = 'block';

                // Load image for cropping
                const reader = new FileReader();
                reader.onload = (e) => {
                    cropImage.src = e.target.result;
                    // FIX: Wait for the image element to fully load before initializing the cropper.
                    cropImage.onload = () => {
                        this.initCropper();
                    };
                };
                reader.readAsDataURL(this.currentFile);
            } else {
                console.log('Crop interface not found, uploading original image');
                this.uploadOriginalImage();
            }
        }
    }

    initCropper() {
        // RECOMMENDED: Destroy any existing cropper instance first for robustness.
        if (this.cropper) {
            this.cropper.destroy();
        }

        let cropImage;

        // For profile images, use the dedicated crop interface
        if (this.options.hideUploadArea) {
            cropImage = document.getElementById('cropImage');
        } else {
            cropImage = document.getElementById(`cropImage_${this.uploadId}`);
        }

        if (!cropImage) {
            console.warn('Crop image element not found');
            return;
        }

        // Initialize Cropper.js if available
        if (typeof Cropper !== 'undefined') {
            this.cropper = new Cropper(cropImage, {
                aspectRatio: this.options.cropAspectRatio,
                viewMode: 1,
                dragMode: 'move',
                autoCropArea: 1,
                restore: false,
                guides: true,
                center: true,
                highlight: false,
                cropBoxMovable: true,
                cropBoxResizable: true,
                toggleDragModeOnDblclick: false,
            });
        } else {
            // Fallback: simple image display without cropping
            console.warn('Cropper.js not loaded. Using simple image display.');
        }
    }

    cropAndSave() {
        if (this.cropper) {
            // Get cropped canvas
            const canvas = this.cropper.getCroppedCanvas({
                width: this.options.cropWidth,
                height: this.options.cropHeight
            });

            // Convert to base64
            const croppedImageData = canvas.toDataURL('image/jpeg', 0.8);

            // Upload cropped image
            this.uploadCroppedImage(croppedImageData);
        } else {
            // Fallback: upload original image
            this.uploadOriginalImage();
        }
    }

    uploadCroppedImage(imageData) {
        const formData = {
            imageData: imageData
        };

        fetch(this.options.croppedUploadUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': this.getAntiForgeryToken()
            },
            body: JSON.stringify(formData)
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    this.showSuccess(data.message);
                    this.updateProfileImage(data.imagePath);
                    this.resetInterface();
                } else {
                    this.showError(data.message);
                }
            })
            .catch(error => {
                console.error('Upload error:', error);
                this.showError('Upload failed. Please try again.');
            });
    }

    uploadOriginalImage() {
        const formData = new FormData();
        formData.append('profilePicture', this.currentFile);

        console.log('Uploading image to:', this.options.uploadUrl);
        console.log('File:', this.currentFile);
        console.log('File name:', this.currentFile.name);
        console.log('File size:', this.currentFile.size);
        console.log('File type:', this.currentFile.type);

        fetch(this.options.uploadUrl, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': this.getAntiForgeryToken()
            },
            body: formData
        })
            .then(response => {
                console.log('Upload response status:', response.status);
                console.log('Upload response headers:', response.headers);
                return response.json();
            })
            .then(data => {
                console.log('Upload response data:', data);
                if (data.success) {
                    this.showSuccess(data.message);
                    this.updateProfileImage(data.imagePath);
                    this.resetInterface();
                } else {
                    this.showError(data.message);
                }
            })
            .catch(error => {
                console.error('Upload error:', error);
                this.showError('Upload failed. Please try again.');
            });
    }

    cancelCrop() {
        this.resetInterface();
    }

    resetInterface() {
        // For profile images, reset the dedicated crop interface
        if (this.options.hideUploadArea) {
            const cropInterface = document.getElementById('cropInterface');
            const profileImageContainer = document.querySelector('.profile-image-container');
            const profilePhotoInput = document.getElementById('profile-photo-input');

            // Reset file input
            if (profilePhotoInput) {
                profilePhotoInput.value = '';
            }

            // Destroy cropper if exists
            if (this.cropper) {
                this.cropper.destroy();
                this.cropper = null;
            }

            // Show profile image container and hide crop interface
            if (profileImageContainer) {
                profileImageContainer.style.display = 'block';
            }
            if (cropInterface) {
                cropInterface.style.display = 'none';
            }
        } else {
            // For regular upload areas
            const uploadZone = document.getElementById(`uploadZone_${this.uploadId}`);
            const cropContainer = document.getElementById(`cropContainer_${this.uploadId}`);
            const fileInput = document.getElementById(`fileInput_${this.uploadId}`);

            // Reset file input
            if (fileInput) {
                fileInput.value = '';
            }

            // Destroy cropper if exists
            if (this.cropper) {
                this.cropper.destroy();
                this.cropper = null;
            }

            // Show upload zone and hide crop container
            if (uploadZone) {
                uploadZone.style.display = 'block';
            }
            if (cropContainer) {
                cropContainer.style.display = 'none';
            }
        }

        this.currentFile = null;
    }

    updateProfileImage(imagePath) {
        console.log('Updating profile image with path:', imagePath);

        // Update profile image display
        const profileImage = document.querySelector('#profile-photo');
        if (profileImage) {
            console.log('Found profile image element, updating src');
            // Add cache-busting parameter to force reload
            const newSrc = imagePath + '?t=' + new Date().getTime();
            profileImage.src = newSrc;

            // Add error handling for the image
            profileImage.onerror = function () {
                console.error('Failed to load profile image:', newSrc);
                this.src = '/images/default-profile.png';
            };

            // Add load success handler
            profileImage.onload = function () {
                console.log('Profile image loaded successfully:', newSrc);
            };
        } else {
            console.warn('Profile image element not found');
        }

        // Also update any other profile images on the page
        const allProfileImages = document.querySelectorAll('.profile-photo');
        allProfileImages.forEach(img => {
            if (img.id !== 'profile-photo') { // Don't update the main one twice
                const newSrc = imagePath + '?t=' + new Date().getTime();
                img.src = newSrc;
            }
        });

        // Reset the profile photo input
        const profilePhotoInput = document.getElementById('profile-photo-input');
        if (profilePhotoInput) {
            profilePhotoInput.value = '';
        }

        // Update navigation bar profile picture if it exists
        const navProfileImage = document.querySelector('#userDropdown img');
        if (navProfileImage) {
            const newSrc = imagePath + '?t=' + new Date().getTime();
            navProfileImage.src = newSrc;
            console.log('Updated navigation bar profile image');
        }

        // Show success message
        this.showSuccess('Profile picture updated successfully!');
    }

    getAntiForgeryToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }

    showSuccess(message) {
        console.log('Success:', message);
        // Show success message
        if (typeof showToast === 'function') {
            showToast('success', message);
        } else {
            // Create a temporary success message
            const successDiv = document.createElement('div');
            successDiv.className = 'alert alert-success alert-dismissible fade show';
            successDiv.style.position = 'fixed';
            successDiv.style.top = '20px';
            successDiv.style.right = '20px';
            successDiv.style.zIndex = '9999';
            successDiv.innerHTML = `
                <i class="fas fa-check-circle me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            `;
            document.body.appendChild(successDiv);

            // Auto-remove after 3 seconds
            setTimeout(() => {
                if (successDiv.parentNode) {
                    successDiv.parentNode.removeChild(successDiv);
                }
            }, 3000);
        }
    }

    showError(message) {
        console.error('Error:', message);
        // Show error message
        if (typeof showToast === 'function') {
            showToast('error', message);
        } else {
            // Create a temporary error message
            const errorDiv = document.createElement('div');
            errorDiv.className = 'alert alert-danger alert-dismissible fade show';
            errorDiv.style.position = 'fixed';
            errorDiv.style.top = '20px';
            errorDiv.style.right = '20px';
            errorDiv.style.zIndex = '9999';
            errorDiv.innerHTML = `
                <i class="fas fa-exclamation-circle me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            `;
            document.body.appendChild(errorDiv);

            // Auto-remove after 5 seconds
            setTimeout(() => {
                if (errorDiv.parentNode) {
                    errorDiv.parentNode.removeChild(errorDiv);
                }
            }, 5000);
        }
    }
}

// Menu Item Image Uploader
class MenuItemImageUploader extends ImageUploader {
    constructor(menuItemId, options = {}) {
        super({
            uploadUrl: `/MenuItems/UploadImage?menuItemId=${menuItemId}`,
            croppedUploadUrl: `/MenuItems/UploadCroppedImage?menuItemId=${menuItemId}`,
            maxFileSize: 10 * 1024 * 1024, // 10MB for menu items
            cropAspectRatio: 4 / 3, // 4:3 aspect ratio for menu items
            cropWidth: 400,
            cropHeight: 300,
            ...options
        });

        this.menuItemId = menuItemId;
    }

    updateProfileImage(imagePath) {
        // Update menu item image display
        const menuItemImage = document.querySelector(`#menuItem-${this.menuItemId} img`);
        if (menuItemImage) {
            menuItemImage.src = imagePath + '?t=' + new Date().getTime();
        }
    }
}