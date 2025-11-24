// Глобальные переменные
let myMap;
let clusterer;

// Точка входа
startApp();

async function startApp() {
    try {
        // 1. Получаем ключ с сервера
        const token = localStorage.getItem('jwt_token');
        const response = await fetch('/api/Config/map-key', {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (!response.ok) throw new Error('Ошибка получения конфига');
        
        const data = await response.json();
        const apiKey = data.key || ''; // Если ключа нет, будет демо-режим

        // 2. Динамически загружаем скрипт Яндекс Карт
        const script = document.createElement('script');
        script.src = `https://api-maps.yandex.ru/2.1/?apikey=${apiKey}&lang=ru_RU`;
        script.type = 'text/javascript';
        
        // Когда скрипт загрузится - инициализируем карту
        script.onload = () => {
            ymaps.ready(init);
        };
        
        document.head.appendChild(script);

    } catch (error) {
        console.error('Critical Error:', error);
        alert('Ошибка инициализации приложения');
    }
}

function init() {
    // 1. Создаем карту
    myMap = new ymaps.Map("map", {
        center: [55.751574, 37.573856], // Центр (Москва)
        zoom: 7,
        controls: ['zoomControl', 'fullscreenControl']
    });

    // 2. Создаем кластеризатор
    clusterer = new ymaps.Clusterer({
        preset: 'islands#invertedVioletClusterIcons',
        groupByCoordinates: false,
        clusterDisableClickZoom: false,
        clusterHideIconOnBalloonOpen: false,
        geoObjectHideIconOnBalloonOpen: false
    });

    myMap.geoObjects.add(clusterer);

    // 3. Загружаем данные
    loadFactories();

    // 4. Настраиваем UI
    document.getElementById('search-btn').addEventListener('click', searchFactories);
    document.getElementById('search-input').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            searchFactories();
        }
    });
}

// Функция загрузки всех заводов
async function loadFactories() {
    try {
        const token = localStorage.getItem('jwt_token');
        const response = await fetch('/api/Factories', {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (response.status === 401) window.location.href = 'login.html';
        const data = await response.json();
        renderPins(data);
    } catch (error) {
        console.error("Ошибка загрузки данных:", error);
        alert("Не удалось загрузить список заводов.");
    }
}

// Функция поиска
async function searchFactories() {
    const query = document.getElementById('search-input').value;
    const url = query ? `/api/Factories/search?q=${encodeURIComponent(query)}` : '/api/Factories';

    try {
        const token = localStorage.getItem('jwt_token');
        const response = await fetch(url, {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (response.status === 401) window.location.href = 'login.html';
        const data = await response.json();
        renderPins(data);
    } catch (error) {
        console.error("Ошибка поиска:", error);
    }
}

// Функция отрисовки меток
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