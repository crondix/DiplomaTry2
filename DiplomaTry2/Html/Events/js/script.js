document.addEventListener("DOMContentLoaded", () => {
  // Получаем все таблицы на странице
  const tables = document.querySelectorAll("table");

  if (tables.length >= 2) {
    // Первая таблица - удаляем четные строки и перемешиваем
    const table1 = tables[0].querySelector("tbody");
    let rows1 = Array.from(table1.querySelectorAll("tr"));

    // Удаляем четные строки (индексы 1, 3, 5 и т.д.)
    rows1 = rows1.filter((_, index) => (index + 1) % 2 !== 0);

    // Перемешиваем оставшиеся строки
    rows1.sort(() => Math.random() - 0.5);

    // Очищаем и добавляем обновленные строки в первую таблицу
    table1.innerHTML = "";
    rows1.forEach(row => table1.appendChild(row));

    // Вторая таблица - удаляем нечетные строки и перемешиваем
    const table2 = tables[1].querySelector("tbody");
    let rows2 = Array.from(table2.querySelectorAll("tr"));

    // Удаляем нечетные строки (индексы 0, 2, 4 и т.д.)
    rows2 = rows2.filter((_, index) => (index + 1) % 2 === 0);

    // Перемешиваем оставшиеся строки
    rows2.sort(() => Math.random() - 0.5);

    // Очищаем и добавляем обновленные строки во вторую таблицу
    table2.innerHTML = "";
    rows2.forEach(row => table2.appendChild(row));
  }
});
