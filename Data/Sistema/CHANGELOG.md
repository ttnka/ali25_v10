# Changelog

## [2024-02-29] - Mejora en Sistema de Claves de Caché

### Backup
- Creado Repo.cs.bak como punto de restauración

### Cambios Planeados
1. Modificar GetCacheKey para reflejar filtros exactos
2. Implementar FilterValueExtractor para extraer valores de filtros
3. Distinguir consultas con/sin filtros en claves de caché

### Archivos Afectados
- Repo.cs (principal)
- Program.cs (si requiere cambios en DI)

## [2024-02-29] - Implementación Nuevo Sistema de Caché

### Cambios Planeados
- Implementar nuevo CacheService
- Simplificar generación de claves de caché
- Mejorar manejo de concurrencia

### Archivos Afectados
- Repo.cs
- Program.cs (para registro de servicios) 