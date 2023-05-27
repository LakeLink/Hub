import { CredentialDeviceType } from "@simplewebauthn/typescript-types";

export interface User {
    verified: boolean;
    role: string;
    realName: string;
    org: string;
    casId: string;
    casPassword: string;
};