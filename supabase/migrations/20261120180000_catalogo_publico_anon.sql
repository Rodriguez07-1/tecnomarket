-- ============================================================================
-- TecnoMarket — catálogo público para visitantes (rol anon de Supabase)
-- ============================================================================
-- La API C# consulta Supabase con la clave publishable, que actúa como rol
-- `anon`. Las políticas existentes solo permiten SELECT a `authenticated`,
-- por eso el catálogo de la Web aparecía vacío si el visitante no iniciaba
-- sesión. Estas políticas son ADDITIVAS: no reemplazan ninguna existente y
-- solo exponen lo que la tarjeta del catálogo necesita mostrar.
-- ============================================================================

begin;

-- 1. Solo las publicaciones aprobadas son visibles para quien no inició sesión.
create policy tm_productos_select_publico
    on public.productos
    for select
    to anon
    using (estado = 'Disponible');

-- 2. Categorías: ya existe política anon; esta la garantiza de forma idempotente.
drop policy if exists tm_categorias_select_publico on public.categorias;
create policy tm_categorias_select_publico
    on public.categorias
    for select
    to anon
    using (true);

-- 3. Vendedores: el visitante solo necesita id, nombre y apellido.
--    Se niega el SELECT amplio y se conceden únicamente esas columnas.
revoke select on public.usuarios from anon;
grant select (id, nombre, apellido) on public.usuarios to anon;

drop policy if exists tm_usuarios_select_publico on public.usuarios;
create policy tm_usuarios_select_publico
    on public.usuarios
    for select
    to anon
    using (true);

commit;
