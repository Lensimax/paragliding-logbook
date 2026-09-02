export interface CurrentUser {
  id: string
  publicId: string
  username: string
  email: string
}

export interface RegisterPayload {
  username: string
  email: string
  password: string
  passwordConfirmation: string
}

export interface LoginPayload {
  email: string
  password: string
  stayConnected: boolean
}
