let myMap;
let clusterer;
let allFactories = []; // Все заводы (для сброса)

// Таймер для задержки поиска (чтобы не ддосить сервер при каждом нажатии клавиши)
let searchTimeout;

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

    // Добавляем поиск адресов (Геокодинг) вручную, чтобы задать позицию
    var searchControl = new ymaps.control.SearchControl({
        options: {
            noPlacemark: false, // Ставить метку
            placeholderContent: 'Адрес (улица, город)...',
            size: 'large',
            float: 'none', // Отключаем стандартное обтекание
            position: { top: 80, right: 10 } // Сдвигаем вправо и вниз (под кнопку Админки)
        }
    });

    myMap.controls.add(searchControl);

    loadFactories(); // Загружаем все заводы изначально

    // --- Обработчики ---
    
    // UI Кнопки и шторка
    const panel = document.getElementById('controls-panel');
    const handler = document.getElementById('mobile-handler');
    const handlerIcon = document.getElementById('mobile-handler-icon');

    handler.addEventListener('click', () => {
        panel.classList.toggle('collapsed');
        const isCollapsed = panel.classList.contains('collapsed');
        handlerIcon.textContent = isCollapsed ? "▲ Показать фильтры" : "▼ Скрыть фильтры";
    });

    document.getElementById('apply-filters-btn').addEventListener('click', () => {
        performSearch(); // Запускаем поиск
        if (window.innerWidth <= 768) {
            panel.classList.add('collapsed');
            handlerIcon.textContent = "▲ Показать фильтры";
        }
    });

    document.getElementById('reset-filters-btn').addEventListener('click', resetFilters);

    // Ввод в поиск (с задержкой 500мс)
    document.getElementById('search-input').addEventListener('input', () => {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(performSearch, 500);
    });

    // Изменение селектов (фильтруем сразу)
    ['factory-filter', 'category-filter', 'vip-filter', 'city-filter'].forEach(id => {
        document.getElementById(id).addEventListener('change', performSearch);
    });

    ['chk-name', 'chk-prod', 'chk-price'].forEach(id => {
        document.getElementById(id).addEventListener('change', performSearch);
    });

    // Добавляем логику для новой кнопки #main-filter-toggle
    const toggleBtn = document.getElementById('main-filter-toggle');

    toggleBtn.addEventListener('click', () => {
        panel.classList.toggle('hidden');
        panel.classList.toggle('active');
        // Меняем текст кнопки
        if (panel.classList.contains('active')) {
            toggleBtn.textContent = "✖ Скрыть фильтры";
            toggleBtn.classList.replace('btn-primary', 'btn-secondary');
        } else {
            toggleBtn.textContent = "🔍 Поиск заводов";
            toggleBtn.classList.replace('btn-secondary', 'btn-primary');
        }
    });
}

// 1. Загрузка всех данных (старт)
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

async function performSearch() {
    const searchText = document.getElementById('search-input').value.trim();
    const token = localStorage.getItem('jwt_token');
    
    // Считываем галочки
    const chkName = document.getElementById('chk-name').checked;
    const chkProd = document.getElementById('chk-prod').checked;
    const chkPrice = document.getElementById('chk-price').checked;
    
    let sourceData = allFactories; // По умолчанию (если поиск пустой)

    if (searchText.length > 0) {
        try {
            // Формируем URL с параметрами
            const params = new URLSearchParams({
                q: searchText,
                searchName: chkName,
                searchProd: chkProd,
                searchPrice: chkPrice
            });

            const response = await fetch(`/api/Factories/search?${params.toString()}`, {
                headers: { 'Authorization': 'Bearer ' + token }
            });
            
            if (response.status === 401) return handleAuthError();
            if (response.ok) {
                sourceData = await response.json();
            }
        } catch (e) {
            console.error("Ошибка поиска на сервере", e);
            return;
        }
    }

    // Применяем локальные фильтры (Город, Завод) к результату поиска
    applyLocalFilters(sourceData);
}

function applyLocalFilters(factoriesToFilter) {
    const factoryVal = document.getElementById('factory-filter').value;
    const categoryVal = document.getElementById('category-filter').value;
    const vipVal = document.getElementById('vip-filter').value;
    const cityVal = document.getElementById('city-filter').value;

    const filtered = factoriesToFilter.filter(f => {
        if (factoryVal && f.name !== factoryVal) return false;
        if (cityVal && (!f.address || !f.address.startsWith(cityVal))) return false;
        if (categoryVal && (!f.productCategories || !f.productCategories.includes(categoryVal))) return false;
        if (vipVal && (!f.vipProducts || !f.vipProducts.includes(vipVal))) return false;
        return true;
    });

    renderPins(filtered);
}

function populateFilters(factories) {
    const factorySelect = document.getElementById('factory-filter');
    const categorySelect = document.getElementById('category-filter');
    const vipSelect = document.getElementById('vip-filter');
    const citySelect = document.getElementById('city-filter');

    const sets = { factories: new Set(), categories: new Set(), vip: new Set(), cities: new Set() };

    factories.forEach(f => {
        if (f.name && f.name.trim()) sets.factories.add(f.name.trim());
        if (f.address) {
            const city = f.address.split(',')[0].trim();
            if (city.length > 2 && isNaN(parseInt(city[0]))) sets.cities.add(city);
        }
        const addClean = (strList, targetSet) => {
            if (!strList) return;
            strList.split(',').forEach(item => {
                const val = item.trim();
                if (val.length > 3 && isNaN(parseInt(val[0]))) targetSet.add(val);
            });
        };
        addClean(f.productCategories, sets.categories);
        addClean(f.vipProducts, sets.vip);
    });

    const fill = (select, set) => {
        Array.from(set).sort().forEach(val => select.add(new Option(val, val)));
    };
    fill(factorySelect, sets.factories);
    fill(categorySelect, sets.categories);
    fill(vipSelect, sets.vip);
    fill(citySelect, sets.cities);
}

function renderPins(factories) {
    clusterer.removeAll();
    const geoObjects = [];
    factories.forEach(f => {
        if (!f.latitude || !f.longitude) return;
        const color = f.isVip ? 'islands#redDotIcon' : 'islands#blueDotIcon';
        const content = `
            <div style="font-size: 14px; line-height: 1.5; min-width: 200px;">
                <strong style="font-size: 16px;">${f.name}</strong><br>
                <hr style="margin: 5px 0; border: 0; border-top: 1px solid #eee;">
                ${f.isVip ? '<span style="color: red; font-weight: bold;">★ VIP Партнер</span><br>' : ''}
                <b>Продукция:</b> ${f.productCategories || 'Нет данных'}<br>
                ${f.phone ? `<b>Контакты:</b> ${f.phone}<br>` : ''}
                ${f.address ? `<b>Адрес:</b> ${f.address}<br>` : ''}
                ${f.priceUrl ? `<br><a href="${f.priceUrl}" target="_blank" style="color: #007bff; font-weight: bold;">Перейти на сайт</a>` : ''}
                ${f.priceListUrl ? `<br><button onclick="downloadPrice(${f.id})" class="btn-download" style="border:none; cursor:pointer; width:100%;">📥 Скачать Прайс (Excel)</button>` : ''}
                ${f.comment ? `<br><br><small style="color: #666">Доп. инфо: ${f.comment}</small>` : ''}
            </div>
        `;
        geoObjects.push(new ymaps.Placemark([f.latitude, f.longitude], { balloonContent: content, hintContent: f.name }, { preset: color }));
    });
    clusterer.add(geoObjects);
    if (geoObjects.length > 0) myMap.setBounds(clusterer.getBounds(), { checkZoomRange: true });
}

async function downloadPrice(id) {
    const token = localStorage.getItem('jwt_token');
    try {
        const response = await fetch(`/api/PriceList/download/${id}`, { headers: { 'Authorization': 'Bearer ' + token } });
        if (response.ok) {
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `Price_Factory_${id}.xlsx`;
            document.body.appendChild(a);
            a.click();
            a.remove();
        } else { alert("Не удалось скачать файл"); }
    } catch (e) { console.error(e); alert("Ошибка сети"); }
}

function resetFilters() {
    document.querySelectorAll('select').forEach(s => s.value = "");
    document.getElementById('search-input').value = "";
    loadFactories(); // Сбрасываем на "Все"
}

function handleAuthError() {
    localStorage.removeItem('jwt_token');
    window.location.href = 'login.html';
}