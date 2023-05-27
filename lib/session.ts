// this file is a wrapper with defaults to be used in both API routes and `getServerSideProps` functions
import type { IronSessionOptions } from "iron-session";

if (!process.env.SESS_PWD) {
  console.warn("Session password not set!")
}

export const sessionOptions: IronSessionOptions = {
  password: process.env.SESS_PWD ?? "complex_password_at_least_32_characters_long",
  cookieName: "lks",
  cookieOptions: {
    secure: process.env.NODE_ENV === "production",
  },
};

// This is where we specify the typings of req.session.*
declare module "iron-session" {
  interface IronSessionData {
    verified: boolean;
    org: string;
    casId: string;
    casPassword: string;
  }
}
