import { CredentialDeviceType } from "@simplewebauthn/typescript-types";

export interface Authenticator {
    // name: String;
    credentialID: Buffer;
    credentialPublicKey: Buffer;
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