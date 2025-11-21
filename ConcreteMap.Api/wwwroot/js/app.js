// Ждем загрузки API Яндекс.Карт
ymaps.ready(init);

let myMap;
let clusterer; // Кластеризатор (группирует метки, если их много рядом)

function init() {
    // 1. Создаем карту
    myMap = new ymaps.Map("map", {
        center: [55.751574, 37.573856], // Центр (Москва)
        zoom: 7,
        controls: ['zoomControl', 'fullscreenControl']
    });

    // 2. Создаем кластеризатор (чтобы метки не накладывались)
    clusterer = new ymaps.Clusterer({
        preset: 'islands#invertedVioletClusterIcons',
        groupByCoordinates: false,
        clusterDisableClickZoom: false,
        clusterHideIconOnBalloonOpen: false,
        geoObjectHideIconOnBalloonOpen: false
    });

    myMap.geoObjects.add(clusterer);

    // 3. Загружаем данные при старте
    loadFactories();

    // 4. Настраиваем кнопку поиска
    document.getElementById('search-btn').addEventListener('click', searchFactories);
    
    // Поиск по Enter
    document.getElementById('search-input').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            searchFactories();
        }
    });
}

// Функция загрузки всех заводов
async function loadFactories() {
    try {
        const response = await fetch('/api/Factories');
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
    
    // Если пусто - грузим всё, иначе ищем
    const url = query ? `/api/Factories/search?q=${encodeURIComponent(query)}` : '/api/Factories';

    try {
        const response = await fetch(url);
        const data = await response.json();
        renderPins(data);
    } catch (error) {
        console.error("Ошибка поиска:", error);
    }
}

// Функция отрисовки меток
function renderPins(factories) {
    // Очищаем старые метки
    clusterer.removeAll();

    const geoObjects = [];

    factories.forEach(factory => {
        // Пропускаем, если координат нет
        if (!factory.latitude || !factory.longitude) return;

        // Формируем цвет метки (VIP = Красный, Обычный = Синий)
        const iconColor = factory.isVip ? 'islands#redDotIcon' : 'islands#blueDotIcon';

        // Формируем содержимое балуна (попапа)
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

        // Создаем метку
        const placemark = new ymaps.Placemark(
            [factory.latitude, factory.longitude], 
            {
                balloonContent: balloonContent,
                hintContent: factory.name
            }, 
            {
                preset: iconColor
            }
        );

        geoObjects.push(placemark);
    });

    // Добавляем массив меток в кластеризатор
    clusterer.add(geoObjects);
    
    // Если нашлись заводы - центрируем карту на них
    if (geoObjects.length > 0) {
        myMap.setBounds(clusterer.getBounds(), { checkZoomRange: true });
    }
}