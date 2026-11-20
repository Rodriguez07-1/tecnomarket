-- Copia de la migración aplicada en Supabase el 2026-11-20.
-- Permite que un producto nuevo nazca en estado 'Pendiente' como exige la RLS.

alter table public.productos
    drop constraint if exists productos_estado_check;

alter table public.productos
    add constraint productos_estado_check
    check (estado in ('Pendiente', 'Disponible', 'Vendido', 'Rechazado', 'Pausado'));
