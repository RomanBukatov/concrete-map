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

    const panel = document.getElementById('controls-panel');
    const handler = document.getElementById('mobile-handler');
    const handlerIcon = document.getElementById('mobile-handler-icon');

    // Переключатель по клику на ручку
    handler.addEventListener('click', () => {
        panel.classList.toggle('collapsed');
        const isCollapsed = panel.classList.contains('collapsed');
        handlerIcon.textContent = isCollapsed ? "▲ Показать фильтры" : "▼ Скрыть фильтры";
    });

    // Обработчик кнопки "Применить"
    document.getElementById('apply-filters-btn').addEventListener('click', () => {
        applyFilters(); // Вызываем основную логику
        // Если мобилка - сворачиваем панель
        if (window.innerWidth <= 768) {
            panel.classList.add('collapsed');
            handlerIcon.textContent = "▲ Показать фильтры";
        }
    });
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

        const iconColor = f.isVip ? 'islands#redDotIcon' : 'islands#blueDotIcon';

        // Формируем красивый контент балуна
        const balloonContent = `
            <div style="font-size: 14px; line-height: 1.5; min-width: 200px;">
                <strong style="font-size: 16px;">${f.name}</strong><br>
                <hr style="margin: 5px 0; border: 0; border-top: 1px solid #eee;">

                ${f.isVip ? '<span style="color: red; font-weight: bold;">★ VIP Партнер</span><br>' : ''}

                <b>Продукция:</b> ${f.productCategories || 'Нет данных'}<br>

                ${f.phone ? `<b>Контакты:</b> ${f.phone}<br>` : ''}
                ${f.address ? `<b>Адрес:</b> ${f.address}<br>` : ''}

                ${f.priceUrl ? `<br><a href="${f.priceUrl}" target="_blank" style="color: #007bff; font-weight: bold;">Перейти на сайт / Прайс</a>` : ''}

                ${f.comment ? `<br><br><small style="color: #666">Доп. инфо: ${f.comment}</small>` : ''}
            </div>
        `;

        const placemark = new ymaps.Placemark(
            [f.latitude, f.longitude],
            {
                balloonContent: balloonContent,
                hintContent: f.name
            },
            { preset: iconColor }
        );

        geoObjects.push(placemark);
    });

    clusterer.add(geoObjects);

    // Центрируем карту только при первой загрузке или поиске, если найдены объекты
    if (geoObjects.length > 0) {
        // Проверка, чтобы не дёргать карту лишний раз (опционально)
         myMap.setBounds(clusterer.getBounds(), { checkZoomRange: true });
    }
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