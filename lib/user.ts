import { CredentialDeviceType } from "@simplewebauthn/typescript-types";
import { Binary } from "mongodb";

export interface Authenticator {
    // name: String;
    credentialID: Binary;
    credentialPublicKey: Binary;
    counter: number;
    // credentialDeviceType: CredentialDeviceType;
    // credentialBackedUp: boolean;
    // transports?: AuthenticatorTransport[];
    // createdAt: Date;
    // updatedAt: Date;
}

export interface User {
    verified: boolean;
    role: string;
    realName: string;
    org: string;
    casId: string;
    casPassword: string;

    authenticators: Authenticator[];
};