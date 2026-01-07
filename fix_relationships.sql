-- Script para agregar relaciones de clave foránea con DeleteBehavior.Restrict
-- Esto evitará que se eliminen las tareas, notas, credenciales y comentarios cuando se actualiza el usuario

USE inventario_db;

-- Primero, verificamos si las foreign keys ya existen y las eliminamos si es necesario
-- (para que el script sea idempotente)

-- Tareas
SET @query = CONCAT('ALTER TABLE `Tareas` DROP FOREIGN KEY IF EXISTS `FK_Tareas_Usuarios_UsuarioId`');
PREPARE stmt FROM @query;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Notas
SET @query = CONCAT('ALTER TABLE `Notas` DROP FOREIGN KEY IF EXISTS `FK_Notas_Usuarios_UsuarioId`');
PREPARE stmt FROM @query;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Credenciales
SET @query = CONCAT('ALTER TABLE `Credenciales` DROP FOREIGN KEY IF EXISTS `FK_Credenciales_Usuarios_UsuarioId`');
PREPARE stmt FROM @query;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Comentarios
SET @query = CONCAT('ALTER TABLE `Comentarios` DROP FOREIGN KEY IF EXISTS `FK_Comentarios_Usuarios_UsuarioId`');
PREPARE stmt FROM @query;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Ahora agregamos las foreign keys con ON DELETE RESTRICT
-- Esto garantiza que no se pueden eliminar usuarios que tengan datos relacionados
-- Y que al actualizar el usuario, no se eliminen sus datos

ALTER TABLE `Tareas`
    ADD CONSTRAINT `FK_Tareas_Usuarios_UsuarioId` 
    FOREIGN KEY (`UsuarioId`) 
    REFERENCES `Usuarios` (`Id`) 
    ON DELETE RESTRICT 
    ON UPDATE CASCADE;

ALTER TABLE `Notas`
    ADD CONSTRAINT `FK_Notas_Usuarios_UsuarioId` 
    FOREIGN KEY (`UsuarioId`) 
    REFERENCES `Usuarios` (`Id`) 
    ON DELETE RESTRICT 
    ON UPDATE CASCADE;

ALTER TABLE `Credenciales`
    ADD CONSTRAINT `FK_Credenciales_Usuarios_UsuarioId` 
    FOREIGN KEY (`UsuarioId`) 
    REFERENCES `Usuarios` (`Id`) 
    ON DELETE RESTRICT 
    ON UPDATE CASCADE;

ALTER TABLE `Comentarios`
    ADD CONSTRAINT `FK_Comentarios_Usuarios_UsuarioId` 
    FOREIGN KEY (`UsuarioId`) 
    REFERENCES `Usuarios` (`Id`) 
    ON DELETE RESTRICT 
    ON UPDATE CASCADE;

-- Crear índice único en el username de Usuarios si no existe
ALTER TABLE `Usuarios`
    ADD UNIQUE INDEX IF NOT EXISTS `IX_Usuarios_Username` (`Username`);
