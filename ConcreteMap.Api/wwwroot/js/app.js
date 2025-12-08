let myMap;
let clusterer;
let allFactories = [];

startApp();

async function startApp() {
    try {
        const token = localStorage.getItem('jwt_token');
        if (!token) return handleAuthError();

        const response = await fetch('/api/Config/map-key', { headers: { 'Authorization': 'Bearer ' + token } });
        if (response.status === 401) return handleAuthError();
        
        const data = await response.json();
        const apiKey = data.key || ''; 

        const script = document.createElement('script');
        script.src = `https://api-maps.yandex.ru/2.1/?apikey=${apiKey}&lang=ru_RU`;
        script.type = 'text/javascript';
        script.onload = () => ymaps.ready(init);
        document.head.appendChild(script);
    } catch (e) { handleAuthError(); }
}

function init() {
    myMap = new ymaps.Map("map", { center: [55.751574, 37.573856], zoom: 7, controls: ['zoomControl', 'fullscreenControl'] });
    clusterer = new ymaps.Clusterer({ preset: 'islands#invertedVioletClusterIcons', groupByCoordinates: false });
    myMap.geoObjects.add(clusterer);

    loadFactories();

    document.getElementById('apply-filters-btn').addEventListener('click', applyFilters);
    document.getElementById('reset-filters-btn').addEventListener('click', resetFilters);
    
    // Живой поиск и фильтры
    const inputs = ['search-input', 'factory-filter', 'category-filter', 'vip-filter', 'city-filter'];
    inputs.forEach(id => document.getElementById(id).addEventListener('change', applyFilters));
    document.getElementById('search-input').addEventListener('input', applyFilters);
}

async function loadFactories() {
    const token = localStorage.getItem('jwt_token');
    const response = await fetch('/api/Factories', { headers: { 'Authorization': 'Bearer ' + token } });
    if (response.status === 401) return handleAuthError();
    if (response.ok) {
        allFactories = await response.json();
        populateFilters(allFactories);
        renderPins(allFactories);
    }
}

function populateFilters(factories) {
    const factorySelect = document.getElementById('factory-filter');
    const categorySelect = document.getElementById('category-filter');
    const vipSelect = document.getElementById('vip-filter');
    const citySelect = document.getElementById('city-filter');

    const sets = {
        factories: new Set(),
        categories: new Set(),
        vip: new Set(),
        cities: new Set()
    };

    factories.forEach(f => {
        if (f.name && f.name.trim()) sets.factories.add(f.name.trim());
        
        if (f.address) {
            const city = f.address.split(',')[0].trim();
            // Фильтр городов: длиннее 2 букв, не начинается с цифры
            if (city.length > 2 && isNaN(parseInt(city[0]))) sets.cities.add(city);
        }

        // Функция очистки категорий
        const addClean = (strList, targetSet) => {
            if (!strList) return;
            strList.split(',').forEach(item => {
                const val = item.trim();
                // ЖЕСТКИЙ ФИЛЬТР: Длина > 3 и НЕ начинается с цифры
                // Это уберет "0 и 1", "18 м", "2 м" и пустые строки
                if (val.length > 3 && isNaN(parseInt(val[0]))) {
                    targetSet.add(val);
                }
            });
        };

        addClean(f.productCategories, sets.categories);
        addClean(f.vipProducts, sets.vip);
    });

    // Сортировка и добавление
    const fill = (select, set) => {
        Array.from(set).sort().forEach(val => select.add(new Option(val, val)));
    };

    fill(factorySelect, sets.factories);
    fill(categorySelect, sets.categories);
    fill(vipSelect, sets.vip);
    fill(citySelect, sets.cities);
}

function applyFilters() {
    const search = document.getElementById('search-input').value.toLowerCase().trim();
    const factoryVal = document.getElementById('factory-filter').value;
    const categoryVal = document.getElementById('category-filter').value;
    const vipVal = document.getElementById('vip-filter').value;
    const cityVal = document.getElementById('city-filter').value;

    const chkName = document.getElementById('chk-name').checked;
    const chkProd = document.getElementById('chk-prod').checked;
    const chkPrice = document.getElementById('chk-price').checked;

    const filtered = allFactories.filter(f => {
        if (factoryVal && f.name !== factoryVal) return false;
        if (cityVal && (!f.address || !f.address.startsWith(cityVal))) return false;
        if (categoryVal && (!f.productCategories || !f.productCategories.includes(categoryVal))) return false;
        if (vipVal && (!f.vipProducts || !f.vipProducts.includes(vipVal))) return false;

        if (search) {
            let match = false;
            if (chkName && f.name?.toLowerCase().includes(search)) match = true;
            if (chkProd && (f.productCategories?.toLowerCase().includes(search) || f.vipProducts?.toLowerCase().includes(search))) match = true;
            if (chkPrice && f.comment?.toLowerCase().includes(search)) match = true; // comment = Вся продукция
            return match;
        }
        return true;
    });

    renderPins(filtered);
}

function renderPins(factories) {
    clusterer.removeAll();
    const geoObjects = [];
    factories.forEach(f => {
        if (!f.latitude || !f.longitude) return;
        const color = f.isVip ? 'islands#redDotIcon' : 'islands#blueDotIcon';
        const content = `<b>${f.name}</b><br><br>Продукция: ${f.productCategories || '-'}<br>${f.priceUrl ? `<a href="${f.priceUrl}" target="_blank">Сайт</a>` : ''}`;
        geoObjects.push(new ymaps.Placemark([f.latitude, f.longitude], { balloonContent: content }, { preset: color }));
    });
    clusterer.add(geoObjects);
    if (geoObjects.length) myMap.setBounds(clusterer.getBounds(), { checkZoomRange: true });
}

function resetFilters() {
    document.querySelectorAll('select').forEach(s => s.value = "");
    document.getElementById('search-input').value = "";
    renderPins(allFactories);
}

function handleAuthError() {
    localStorage.removeItem('jwt_token');
    window.location.href = 'login.html';
}