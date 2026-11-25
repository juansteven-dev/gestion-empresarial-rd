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

interface CsrfTokenResponse {
  requestToken: string
}

let csrfRequestToken: string | null = null
let csrfRequest: Promise<string> | null = null

async function getCsrfRequestToken(): Promise<string> {
  if (csrfRequestToken) return csrfRequestToken
  if (!csrfRequest) {
    csrfRequest = apiClient
      .get<CsrfTokenResponse>('/auth/csrf')
      .then(({ data }) => {
        csrfRequestToken = data.requestToken
        return data.requestToken
      })
      .finally(() => {
        csrfRequest = null
      })
  }

  return csrfRequest
}

apiClient.interceptors.request.use(async (config) => {
  const method = (config.method ?? 'get').toLowerCase()
  if (!['get', 'head', 'options', 'trace'].includes(method)) {
    config.headers.set('X-CSRF-TOKEN', await getCsrfRequestToken())
  }

  return config
})

export function resetCsrfRequestToken(): void {
  csrfRequestToken = null
}

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
