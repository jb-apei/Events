import axios from 'axios'

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  expiresAt: string
}

export const login = async (credentials: LoginRequest): Promise<LoginResponse> => {
  const response = await axios.post<LoginResponse>('/api/auth/login', credentials)
  return response.data
}
