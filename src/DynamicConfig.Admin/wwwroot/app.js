// API Base URL
const API_BASE = '/api/configurations';

// Global variables
let configurations = [];
let currentEditingId = null;

// DOM Content Loaded - Sayfa yüklendiğinde yapılacaklar
document.addEventListener('DOMContentLoaded', function() {
    loadConfigurations();
    setupEventListeners();
});

function setupEventListeners() {
    document.getElementById('nameFilter').addEventListener('keyup', function(e) {
        if (e.key === 'Enter') {
            loadConfigurations();
        }
    });

    document.getElementById('applicationFilter').addEventListener('change', loadConfigurations);
    document.getElementById('statusFilter').addEventListener('change', loadConfigurations);
}

async function loadConfigurations() {
    try {
        showLoading(true);
        hideError();

        const nameFilter = document.getElementById('nameFilter').value;
        const applicationFilter = document.getElementById('applicationFilter').value;
        const statusFilter = document.getElementById('statusFilter').value;

        let url = API_BASE;
        if (nameFilter) {
            url += `?name=${encodeURIComponent(nameFilter)}`;
        }

        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        configurations = await response.json();

        let filteredConfigs = configurations;

        if (applicationFilter) {
            filteredConfigs = filteredConfigs.filter(c => c.applicationName === applicationFilter);
        }

        if (statusFilter !== '') {
            const isActive = statusFilter === 'true';
            filteredConfigs = filteredConfigs.filter(c => c.isActive === isActive);
        }

        displayConfigurations(filteredConfigs);
        updateApplicationFilter();

    } catch (error) {
        console.error('Error loading configurations:', error);
        showError('Konfigürasyonlar yüklenirken hata oluştu: ' + error.message);
    } finally {
        showLoading(false);
    }
}

function displayConfigurations(configs) {
    const container = document.getElementById('configurationsContainer');
    
    if (configs.length === 0) {
        container.innerHTML = `
            <div class="alert alert-info text-center">
                <i class="fas fa-info-circle"></i>
                Hiç konfigürasyon bulunamadı.
            </div>
        `;
        return;
    }

    const html = configs.map(config => `
        <div class="card configuration-card ${!config.isActive ? 'inactive' : ''}">
            <div class="card-body">
                <div class="row">
                    <div class="col-md-8">
                        <h5 class="card-title">
                            ${config.name}
                            <span class="badge bg-secondary type-badge">${config.type}</span>
                            ${config.isActive ? 
                                '<span class="badge bg-success ms-2">Aktif</span>' : 
                                '<span class="badge bg-danger ms-2">Pasif</span>'
                            }
                        </h5>
                        <p class="card-text">
                            <strong>Değer:</strong> <code>${escapeHtml(config.value)}</code><br>
                            <strong>Uygulama:</strong> ${config.applicationName}<br>
                            <small class="text-muted">
                                Son Güncelleme: ${formatDate(config.lastUpdatedAt)}
                            </small>
                        </p>
                    </div>
                    <div class="col-md-4 text-end">
                        <div class="action-buttons">
                            <button class="btn btn-sm btn-outline-primary" onclick="editConfiguration('${config.id}')">
                                <i class="fas fa-edit"></i> Düzenle
                            </button>
                            <button class="btn btn-sm btn-outline-danger" onclick="deleteConfiguration('${config.id}', '${config.name}')">
                                <i class="fas fa-trash"></i> Sil
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `).join('');

    container.innerHTML = html;
}

function updateApplicationFilter() {
    const select = document.getElementById('applicationFilter');
    const currentValue = select.value;
    
    // Unique application names
    const apps = [...new Set(configurations.map(c => c.applicationName))].sort();
    
    select.innerHTML = '<option value="">Tüm Uygulamalar</option>';
    apps.forEach(app => {
        const option = document.createElement('option');
        option.value = app;
        option.textContent = app;
        select.appendChild(option);
    });
    
    select.value = currentValue;
}

// Yeni konfigürasyon modalını göster
function showAddModal() {
    currentEditingId = null;
    document.getElementById('modalTitle').textContent = 'Yeni Konfigürasyon';
    document.getElementById('configForm').reset();
    document.getElementById('configIsActive').checked = true;
    
    const modal = new bootstrap.Modal(document.getElementById('configModal'));
    modal.show();
}

async function editConfiguration(id) {
    try {
        const response = await fetch(`${API_BASE}/${id}`);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const config = await response.json();
        
        currentEditingId = id;
        document.getElementById('modalTitle').textContent = 'Konfigürasyon Düzenle';
        document.getElementById('configId').value = config.id;
        document.getElementById('configName').value = config.name;
        document.getElementById('configType').value = config.type;
        document.getElementById('configValue').value = config.value;
        document.getElementById('configApplication').value = config.applicationName;
        document.getElementById('configIsActive').checked = config.isActive;
        
        const modal = new bootstrap.Modal(document.getElementById('configModal'));
        modal.show();

    } catch (error) {
        console.error('Error loading configuration:', error);
        showError('Konfigürasyon yüklenirken hata oluştu: ' + error.message);
    }
}

async function saveConfiguration() {
    try {
        const form = document.getElementById('configForm');
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const config = {
            name: document.getElementById('configName').value,
            type: document.getElementById('configType').value,
            value: document.getElementById('configValue').value,
            applicationName: document.getElementById('configApplication').value,
            isActive: document.getElementById('configIsActive').checked
        };

        let url = API_BASE;
        let method = 'POST';

        if (currentEditingId) {
            url += `/${currentEditingId}`;
            method = 'PUT';
        }

        const response = await fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(config)
        });

        if (!response.ok) {
            const errorData = await response.text();
            throw new Error(`HTTP error! status: ${response.status}, message: ${errorData}`);
        }

        const modal = bootstrap.Modal.getInstance(document.getElementById('configModal'));
        modal.hide();

        await loadConfigurations();

        showSuccess(currentEditingId ? 'Konfigürasyon güncellendi!' : 'Konfigürasyon eklendi!');

    } catch (error) {
        console.error('Error saving configuration:', error);
        showError('Konfigürasyon kaydedilirken hata oluştu: ' + error.message);
    }
}

async function deleteConfiguration(id, name) {
    if (!confirm(`"${name}" konfigürasyonunu silmek istediğinizden emin misiniz?`)) {
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/${id}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        await loadConfigurations();

        showSuccess('Konfigürasyon silindi!');

    } catch (error) {
        console.error('Error deleting configuration:', error);
        showError('Konfigürasyon silinirken hata oluştu: ' + error.message);
    }
}

function showLoading(show) {
    const loading = document.querySelector('.loading');
    loading.style.display = show ? 'block' : 'none';
}

function showError(message) {
    const errorDiv = document.querySelector('.error-message');
    const errorText = errorDiv.querySelector('.error-text');
    errorText.textContent = message;
    errorDiv.style.display = 'block';
    
    setTimeout(() => {
        errorDiv.style.display = 'none';
    }, 5000);
}

function hideError() {
    const errorDiv = document.querySelector('.error-message');
    errorDiv.style.display = 'none';
}

function showSuccess(message) {
    const alertDiv = document.createElement('div');
    alertDiv.className = 'alert alert-success alert-dismissible fade show';
    alertDiv.innerHTML = `
        <i class="fas fa-check-circle"></i> ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.querySelector('.container').insertBefore(alertDiv, document.querySelector('.filter-section'));
    
    setTimeout(() => {
        alertDiv.remove();
    }, 3000);
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleString('tr-TR');
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
} 