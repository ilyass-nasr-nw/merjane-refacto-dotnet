### Consignes .NET: 
* Ignorez les migrations BDD
* Ne pas modifier les classes qui ont un commentaire: `// WARN: Should not be changed during the exercise
`
* Pour lancer les tests: `dotnet test`

---

## Refactoring — résumé

Ce projet a été refactoré dans le cadre de l'exercice technique Merjane. L'objectif : éliminer un bug fonctionnel (commande introuvable → 500 au lieu de 404), corriger une incohérence de logique, et restructurer le code pour respecter le principe de responsabilité unique (SRP) et une séparation claire des couches — **sans changer le comportement métier existant**.

Le détail exhaustif des changements est dans [`CHANGELOG.md`](./CHANGELOG.md).

### Architecture

```
Controllers/OrdersController.cs        → couche présentation : reçoit la requête HTTP, renvoie 200/404
Services/IOrderService.cs + Impl/       → couche application : orchestre le traitement d'une commande
Services/Stock/
  IProductStockHandler.cs               → contrat commun "traiter un produit"
  IProductStockHandlerResolver.cs/Impl  → sélectionne le bon handler selon le type de produit
  Handlers/
    NormalProductStockHandler.cs        → règle métier NORMAL
    SeasonalProductStockHandler.cs      → règle métier SEASONAL
    ExpirableProductStockHandler.cs     → règle métier EXPIRABLE
Entities/ProductType.cs (enum)          → remplace les chaînes "NORMAL"/"SEASONAL"/"EXPIRABLE"
```

Chaque type de produit a son propre handler (Strategy pattern). Le contrôleur ne connaît que `IOrderService` ; `OrderService` ne connaît aucune règle métier spécifique à un type de produit ; les handlers ne connaissent ni la persistance ni HTTP. Ajouter un nouveau type de produit ne nécessite pas de modifier le code existant : il suffit d'ajouter un handler et de l'enregistrer dans `Program.cs`.

`ProductService.cs` (original) a été supprimé : ses responsabilités ont été redistribuées entre `OrderService` (orchestration) et les trois handlers (règles métier).

### Tests

- Tests unitaires par handler : chaque règle métier (NORMAL / SEASONAL / EXPIRABLE) est testée isolément, sans base de données.
- Tests du resolver et de `OrderService` (commande introuvable, dispatch, sauvegarde).
- Tests d'intégration du contrôleur enrichis (valeurs `Available` réelles, notifications envoyées, cas 404).

`MyUnitTests.cs`, qui testait exclusivement l'ancien `ProductService`, a été remplacé par ces nouveaux tests puisque cette classe n'existe plus dans la nouvelle architecture.
