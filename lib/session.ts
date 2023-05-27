// this file is a wrapper with defaults to be used in both API routes and `getServerSideProps` functions
import type { IronSessionOptions } from "iron-session";
import crypto from 'node:crypto'

let globalWithRandomArray = global as typeof globalThis & {
  _randomArray?: Uint8Array
}

if (!globalWithRandomArray._randomArray) {

  const _randomArray = new Uint8Array(32)
  crypto.getRandomValues(_randomArray)

  globalWithRandomArray._randomArray = _randomArray
}

export const sessionOptions: IronSessionOptions = {
  password: Buffer.from(globalWithRandomArray._randomArray).toString('base64'),
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
