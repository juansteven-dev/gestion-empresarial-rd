import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createUser, getUsers } from '../api/management'
import { getApiErrorMessage } from '../api/httpClient'

const allowedRoles = ['Administrador', 'Empleado', 'EncargadoAlmacen']

export function UsersPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [formOpen, setFormOpen] = useState(false)
  const [successMessage, setSuccessMessage] = useState('')
  const query = useQuery({
    queryKey: ['users', search, page],
    queryFn: () => getUsers({ search, page, pageSize: 10 }),
  })
  const createMutation = useMutation({
    mutationFn: createUser,
    onSuccess: async (createdUser) => {
      setSuccessMessage(`La cuenta ${createdUser.userName} fue creada con el rol ${createdUser.roles.join(', ')}.`)
      setFormOpen(false)
      await queryClient.invalidateQueries({ queryKey: ['users'] })
    },
  })

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const values = new FormData(event.currentTarget)
    createMutation.mutate({
      userName: String(values.get('userName') ?? '').trim(),
      email: String(values.get('email') ?? '').trim(),
      password: String(values.get('password') ?? ''),
      role: String(values.get('role') ?? ''),
    })
  }

  return (
    <section className="catalog-page">
      <div className="page-heading"><div><p className="eyebrow">Administración</p><h1>Usuarios</h1><p className="page-description">Crea cuentas con Identity y consulta los roles y el estado de bloqueo.</p></div><button className="primary-button" type="button" onClick={() => { setFormOpen((open) => !open); createMutation.reset(); setSuccessMessage('') }}>Crear usuario</button></div>
      {successMessage && <p className="form-success" role="status">{successMessage}</p>}
      {formOpen && <form className="catalog-form" onSubmit={submit}>
        <div className="form-heading"><h2>Nueva cuenta</h2><button className="text-button" type="button" onClick={() => setFormOpen(false)}>Cerrar</button></div>
        <div className="form-grid">
          <label>Usuario<input name="userName" required minLength={3} maxLength={256} autoComplete="off" /></label>
          <label>Correo<input name="email" type="email" required maxLength={254} autoComplete="email" /></label>
          <label>Contraseña inicial<input name="password" type="password" required minLength={12} autoComplete="new-password" /></label>
          <label>Rol<select name="role" required defaultValue=""><option value="" disabled>Selecciona un rol</option>{allowedRoles.map((role) => <option value={role} key={role}>{role}</option>)}</select></label>
        </div>
        <p className="form-hint">La contraseña la valida y administra ASP.NET Core Identity. No se registra en esta pantalla.</p>
        {createMutation.isError && <p className="form-error" role="alert">{getApiErrorMessage(createMutation.error)}</p>}
        <button className="primary-button" type="submit" disabled={createMutation.isPending}>{createMutation.isPending ? 'Creando…' : 'Crear cuenta'}</button>
      </form>}
      <div className="catalog-toolbar"><label className="toolbar-search">Buscar usuario o correo<input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} /></label></div>
      {query.isPending ? <p className="catalog-message" role="status">Cargando usuarios…</p>
        : query.isError ? <p className="catalog-message error-text" role="alert">{getApiErrorMessage(query.error)}</p>
          : query.data.items.length === 0 ? <p className="catalog-message">No hay usuarios que coincidan con la búsqueda.</p>
            : <div className="table-wrap"><table className="catalog-table"><thead><tr><th>Usuario</th><th>Correo</th><th>Roles</th><th>Estado Identity</th></tr></thead><tbody>
              {query.data.items.map((user) => <tr key={user.id}><td><strong>{user.userName}</strong><small>{user.id}</small></td><td>{user.email || '—'}</td><td>{user.roles.join(', ') || 'Sin rol'}</td><td><span className={`status-pill ${user.isLockedOut ? 'warning' : 'active'}`}>{user.isLockedOut ? 'Bloqueado' : 'Disponible'}</span></td></tr>)}
            </tbody></table></div>}
      {query.data && <div className="pagination"><span>{query.data.totalCount} usuarios · página {query.data.page} de {Math.max(query.data.totalPages, 1)}</span><div><button className="secondary-button" type="button" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Anterior</button><button className="secondary-button" type="button" disabled={page >= query.data.totalPages} onClick={() => setPage((value) => value + 1)}>Siguiente</button></div></div>}
    </section>
  )
}
