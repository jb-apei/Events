import axios from 'axios'

// Use the same API URL as prospects.ts
const apiUrl = (import.meta as any).env?.VITE_API_URL || 'https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/api'

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  expiresAt: string
}

export const login = async (credentials: LoginRequest): Promise<LoginResponse> => {
  const response = await axios.post<LoginResponse>(`${apiUrl}/auth/login`, credentials)
  return response.data
}
