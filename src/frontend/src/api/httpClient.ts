import axios from 'axios'

interface ProblemDetails {
  title?: unknown
  detail?: unknown
}

export const apiClient = axios.create({
  baseURL: '/api/v1',
  withCredentials: true,
  headers: { Accept: 'application/json' },
})

export function getApiErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const responseData: unknown = error.response?.data
    if (responseData && typeof responseData === 'object') {
      const problem = responseData as ProblemDetails
      if (typeof problem.detail === 'string') return problem.detail
      if (typeof problem.title === 'string') return problem.title
    }

    return error.message || 'No se pudo completar la solicitud.'
  }

  return error instanceof Error ? error.message : 'Ocurrió un error inesperado.'
}
