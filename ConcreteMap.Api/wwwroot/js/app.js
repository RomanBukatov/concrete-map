// Глобальные переменные
let myMap;
let clusterer;

// Точка входа
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

    document.getElementById('search-btn').addEventListener('click', searchFactories);
    document.getElementById('search-input').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') searchFactories();
    });
}

async function loadFactories() {
    try {
        const token = localStorage.getItem('jwt_token');
        const response = await fetch('/api/Factories', {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        
        if (response.status === 401) {
            handleAuthError();
            return;
        }

        if (response.ok) {
            const data = await response.json();
            renderPins(data);
        }
    } catch (error) {
        console.error("Ошибка загрузки данных:", error);
    }
}

async function searchFactories() {
    const query = document.getElementById('search-input').value;
    const url = query ? `/api/Factories/search?q=${encodeURIComponent(query)}` : '/api/Factories';

    try {
        const token = localStorage.getItem('jwt_token');
        const response = await fetch(url, {
            headers: { 'Authorization': 'Bearer ' + token }
        });

        if (response.status === 401) {
            handleAuthError();
            return;
        }

        if (response.ok) {
            const data = await response.json();
            renderPins(data);
        }
    } catch (error) {
        console.error("Ошибка поиска:", error);
    }
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