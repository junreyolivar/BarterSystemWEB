// BarterPro custom JavaScript

// Auto-dismiss alerts after 5 seconds
document.addEventListener('DOMContentLoaded', function () {
    // Auto-dismiss alerts
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });

    // Form validation enhancements (EXCLUDE chat form from this)
    const forms = document.querySelectorAll('form:not(#messageForm)');
    forms.forEach(form => {
        form.addEventListener('submit', function () {
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Processing...';
            }
        });
    });

    // Image error handling
    const images = document.querySelectorAll('img');
    images.forEach(img => {
        img.addEventListener('error', function () {
            this.src = '/images/no-image.jpg';
        });
    });

    // CHAT FUNCTIONALITY - ADDED
    initializeChat();
});

// CHAT SPECIFIC FUNCTIONS
function initializeChat() {
    const messageForm = document.getElementById('messageForm');
    if (!messageForm) return;

    // Auto-scroll to bottom of chat
    scrollToBottom();

    // Send message functionality
    messageForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        const messageInput = document.getElementById('messageInput');
        const receiverIdInput = document.getElementById('receiverId');

        if (!receiverIdInput) {
            alert('Please select a user to chat with first');
            return;
        }

        const receiverId = receiverIdInput.value;
        const content = messageInput.value.trim();

        if (!content) {
            alert('Please enter a message');
            return;
        }

        if (!receiverId || receiverId === '0') {
            alert('Please select a user to chat with first');
            return;
        }

        // Show loading state
        const submitBtn = messageForm.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Sending...';
        submitBtn.disabled = true;

        try {
            // Get anti-forgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            const formData = new FormData();
            formData.append('receiverId', receiverId);
            formData.append('content', content);
            if (token) {
                formData.append('__RequestVerificationToken', token);
            }

            const response = await fetch('/Chat/SendMessage', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                messageInput.value = '';
                window.location.reload();
            } else {
                debugger;
                alert(result.error || 'Successfully sending message');
             
            }
        } catch (error) {
            console.error('Error:', error);
            
        }
    });

    const receiverIdInput = document.getElementById('receiverId');
    if (receiverIdInput && receiverIdInput.value > 0) {
        setInterval(async () => {
            try {
                const response = await fetch(`/Chat/GetMessages?userId=${receiverIdInput.value}`);
                const result = await response.json();

                if (result.success) {
                    // You can implement real-time message updates here
                    updateChatBadge();
                }
            } catch (error) {
                console.error('Error fetching messages:', error);
            }
        }, 5000);
    }
}

// Auto-scroll to bottom of chat
function scrollToBottom() {
    const chatMessages = document.getElementById('chatMessages');
    if (chatMessages) {
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }
}

// Utility functions
const BarterPro = {
    // Show loading state
    showLoading: function (button) {
        button.disabled = true;
        const originalText = button.innerHTML;
        button.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Loading...';
        return originalText;
    },

    // Hide loading state
    hideLoading: function (button, originalText) {
        button.disabled = false;
        button.innerHTML = originalText;
    },

    // Format date
    formatDate: function (dateString) {
        const options = { year: 'numeric', month: 'short', day: 'numeric' };
        return new Date(dateString).toLocaleDateString(undefined, options);
    },

    // Show toast notification
    showToast: function (message, type = 'info') {
        // You can implement a toast notification system here
        console.log(`${type.toUpperCase()}: ${message}`);
    }
};

// Update chat badge
function updateChatBadge() {
    fetch('/Chat/GetUnreadCount')
        .then(response => response.json())
        .then(data => {
            const badge = document.getElementById('chatBadge');
            if (badge && data.count > 0) {
                badge.textContent = data.count;
                badge.style.display = 'inline';
            } else if (badge) {
                badge.style.display = 'none';
            }
        })
        .catch(error => console.error('Error fetching chat count:', error));
}

// Update pending badge (if exists)
function updatePendingBadge() {
    const badge = document.getElementById('pendingBadge');
    if (badge) {
        // Add your pending count logic here if needed
    }
}

// Update all badges on page load and periodically
document.addEventListener('DOMContentLoaded', function () {
    updatePendingBadge();
    updateChatBadge();
    setInterval(updatePendingBadge, 30000);
    setInterval(updateChatBadge, 30000);
});