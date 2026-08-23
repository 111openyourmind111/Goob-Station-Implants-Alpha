/// <summary>
/// Прототипи імплантів для кіберпсихозу
/// 
/// СТРУКТУРА:
/// - id: Унікальний ідентифікатор
/// - name: Назва для гравців
/// - description: Опис ефектів
/// - components:
///   - ImplantSanityComponent: Параметри впливу на свідомість
///     - ImplantName: Назва для логів системи
///     - InstallationSanityDamage: Шкода при встановленні
///     - ActiveUsageSanityDamage: Шкода при активації (за сек)
///     - PassiveSanityDamage: Пасивна шкода (за сек)
/// 
/// ПРИКЛАД:
/// - type: Entity
///   id: ImplantBerserker
///   name: Бойовий імплант
///   description: "Збільшує силу, але гризе розум"
///   components:
///     - type: ImplantSanityComponent
///       ImplantName: "Berserker Combat Implant"
///       InstallationSanityDamage: 10
///       ActiveUsageSanityDamage: 2.0
///       PassiveSanityDamage: 0.5
/// </summary>

// TODO: Додати конкретні імпланти по мірі розробки
// Структура файлу: YAML прототипи для Resources/Prototypes/
