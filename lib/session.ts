// this file is a wrapper with defaults to be used in both API routes and `getServerSideProps` functions
import type { IronSessionOptions } from "iron-session";

const _randomArray = new Uint8Array(32)
crypto.getRandomValues(_randomArray)

export const sessionOptions: IronSessionOptions = {
  password: Buffer.from(_randomArray).toString('base64'),
  cookieName: "lks",
  ttl: 60 * 5,
  cookieOptions: {
    secure: process.env.NODE_ENV === "production",
  },
};

// This is where we specify the typings of req.session.*
declare module "iron-session" {
  interface IronSessionData {
    userId: string;
    challenge?: string;
  }
}
