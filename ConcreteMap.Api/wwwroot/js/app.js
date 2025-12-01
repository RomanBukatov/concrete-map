// Глобальные переменные
let myMap;
let clusterer;
let allFactories = []; // Хранилище всех заводов

startApp();

async function startApp() {
    try {
        const token = localStorage.getItem('jwt_token');
        
        // Если токена нет локально
        if (!token) {
            handleAuthError();
            return;
        }

        // Запрашиваем ключ карт (это заодно проверяет валидность токена на сервере)
        const response = await fetch('/api/Config/map-key', {
            headers: { 'Authorization': 'Bearer ' + token }
        });

        // ВАЖНО: Если сервер ответил 401 (Unauthorized) - сразу выходим
        if (response.status === 401) {
            console.warn("Токен невалиден (401). Редирект.");
            handleAuthError();
            return;
        }

        // Если другая ошибка сервера
        if (!response.ok) {
            console.error("Ошибка сервера:", response.status);
            // Не показываем алерт, просто кидаем на логин, так безопаснее для UX
            handleAuthError(); 
            return;
        }
        
        const data = await response.json();
        const apiKey = data.key || ''; 

        // Грузим Яндекс Карты
        const script = document.createElement('script');
        script.src = `https://api-maps.yandex.ru/2.1/?apikey=${apiKey}&lang=ru_RU`;
        script.type = 'text/javascript';
        
        script.onload = () => {
            ymaps.ready(init);
        };
        
        script.onerror = () => {
            console.error("Не удалось загрузить скрипт Яндекс.Карт");
        };

        document.head.appendChild(script);

    } catch (error) {
        console.error('Critical Init Error:', error);
        // Если упало по сети или другой причине - всё равно кидаем на логин
        handleAuthError();
    }
}

function init() {
    myMap = new ymaps.Map("map", {
        center: [55.751574, 37.573856],
        zoom: 7,
        controls: ['zoomControl', 'fullscreenControl']
    });

    clusterer = new ymaps.Clusterer({
        preset: 'islands#invertedVioletClusterIcons',
        groupByCoordinates: false,
        clusterDisableClickZoom: false,
        clusterHideIconOnBalloonOpen: false,
        geoObjectHideIconOnBalloonOpen: false
    });

    myMap.geoObjects.add(clusterer);

    loadFactories();

    // Новые обработчики
    document.getElementById('apply-filters-btn').addEventListener('click', applyFilters);
    document.getElementById('reset-filters-btn').addEventListener('click', resetFilters);
}

// Загрузка всех заводов (немного изменена)
async function loadFactories() {
    try {
        const token = localStorage.getItem('jwt_token');
        const response = await fetch('/api/Factories', {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (response.status === 401) return handleAuthError();

        if (response.ok) {
            allFactories = await response.json();
            populateFilters(allFactories); // Заполняем фильтры
            renderPins(allFactories); // Отрисовываем всё
        }
    } catch (error) { console.error("Ошибка загрузки данных:", error); }
}

// НОВАЯ ФУНКЦИЯ: Заполнение фильтров
function populateFilters(factories) {
    const cityFilter = document.getElementById('city-filter');
    const categoryFilter = document.getElementById('category-filter');

    const cities = new Set();
    const categories = new Set();

    factories.forEach(f => {
        // Парсим город (первое слово до запятой)
        if (f.address) {
            const cityMatch = f.address.split(',')[0].trim();
            if (cityMatch) cities.add(cityMatch);
        }
        // Парсим категории
        if (f.productCategories) {
            f.productCategories.split(',').forEach(cat => {
                if(cat.trim()) categories.add(cat.trim());
            });
        }
    });

    cities.forEach(city => cityFilter.add(new Option(city, city)));
    categories.forEach(cat => categoryFilter.add(new Option(cat, cat)));
}

// НОВАЯ ФУНКЦИЯ: Применение всех фильтров
function applyFilters() {
    const searchText = document.getElementById('search-input').value.toLowerCase();
    const city = document.getElementById('city-filter').value;
    const category = document.getElementById('category-filter').value;

    let filteredFactories = allFactories;

    // 1. Фильтр по городу
    if (city) {
        filteredFactories = filteredFactories.filter(f => f.address && f.address.startsWith(city));
    }
    // 2. Фильтр по категории
    if (category) {
        filteredFactories = filteredFactories.filter(f => f.productCategories && f.productCategories.includes(category));
    }
    // 3. Фильтр по поиску
    if (searchText) {
        filteredFactories = filteredFactories.filter(f => 
            (f.name && f.name.toLowerCase().includes(searchText)) ||
            (f.comment && f.comment.toLowerCase().includes(searchText)) ||
            (f.productCategories && f.productCategories.toLowerCase().includes(searchText))
        );
    }

    renderPins(filteredFactories);
}

// НОВАЯ ФУНКЦИЯ: Сброс фильтров
function resetFilters() {
    document.getElementById('search-input').value = "";
    document.getElementById('city-filter').value = "";
    document.getElementById('category-filter').value = "";
    renderPins(allFactories); // Показываем снова все
}

function renderPins(factories) {
    clusterer.removeAll();
    const geoObjects = [];

    factories.forEach(factory => {
        if (!factory.latitude || !factory.longitude) return;

        const iconColor = factory.isVip ? 'islands#redDotIcon' : 'islands#blueDotIcon';
        const balloonContent = `
            <div style="font-size: 14px; line-height: 1.5;">
                <strong style="font-size: 16px;">${factory.name}</strong><br>
                <hr style="margin: 5px 0; border: 0; border-top: 1px solid #eee;">
                ${factory.isVip ? '<span style="color: red; font-weight: bold;">★ VIP Партнер</span><br>' : ''}
                <b>Продукция:</b> ${factory.productCategories || 'Нет данных'}<br>
                ${factory.phone ? `<b>Тел:</b> ${factory.phone}<br>` : ''}
                ${factory.address ? `<b>Адрес:</b> ${factory.address}<br>` : ''}
                ${factory.priceUrl ? `<br><a href="${factory.priceUrl}" target="_blank" style="color: #007bff">Перейти на сайт / Прайс</a>` : ''}
                ${factory.comment ? `<br><br><small style="color: #666">${factory.comment}</small>` : ''}
            </div>
        `;

        const placemark = new ymaps.Placemark(
            [factory.latitude, factory.longitude], 
            { balloonContent: balloonContent, hintContent: factory.name }, 
            { preset: iconColor }
        );

        geoObjects.push(placemark);
    });

    clusterer.add(geoObjects);
    
    if (geoObjects.length > 0) {
        myMap.setBounds(clusterer.getBounds(), { checkZoomRange: true });
    }
}

function handleAuthError() {
    // Удаляем всё и редиректим. Никаких алертов!
    localStorage.removeItem('jwt_token');
    localStorage.removeItem('user_role');
    window.location.href = 'login.html';
}