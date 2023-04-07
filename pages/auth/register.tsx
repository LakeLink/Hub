import { FormEvent, useEffect, useState } from "react";
import { withIronSessionSsr } from "iron-session/next";
import { sessionOptions } from "~/lib/session";
import { useRouter } from "next/router";
import { browserSupportsWebAuthn, startRegistration } from '@simplewebauthn/browser'
import { generateRegistrationOptions } from "@simplewebauthn/server";
import { ObjectId } from "mongodb";
import mongo from "~/lib/mongo";
import { User } from "~/lib/user";
import { InferGetServerSidePropsType } from "next/types";
import { genRegistration } from "~/lib/webauthn";

export default function Register({ options }: InferGetServerSidePropsType<typeof getServerSideProps>) {
  const router = useRouter();
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [error, setError] = useState("");
  const [isAvailable, setIsAvailable] = useState<boolean | null>(null);

  useEffect(() => {
    const checkAvailability = async () => {
      const available =
        browserSupportsWebAuthn();
      setIsAvailable(available);
    };

    checkAvailability();
  }, []);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();

    console.log(options)
    let attResp = await startRegistration(options);
    console.log(attResp)
    const verificationResp = await fetch("/api/auth/register", {
      method: "POST",
      body: JSON.stringify(attResp),
      headers: {
        "Content-Type": "application/json",
      },
    });

    const verificationJSON = await verificationResp.json();
    console.log(verificationJSON)
  };

  return (
    <>
      <h1>Register Account</h1>
      {isAvailable ? (
        <form method="POST" onSubmit={onSubmit}>
          <input
            type="text"
            id="username"
            name="username"
            placeholder="Username"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
          />
          {/* <input
            type="email"
            id="email"
            name="email"
            placeholder="Email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          /> */}
          <input type="submit" value="Register" />
          {error != null ? <pre>{error}</pre> : null}
        </form>
      ) : (
        <p>Sorry, webauthn is not available.</p>
      )}
    </>
  );
}

export const getServerSideProps = withIronSessionSsr(async function ({
  req,
  res,
}) {
  const col = (await mongo).db('lakehub').collection('users')
  const user = await col.findOne<User>({ _id: new ObjectId(req.session.userId) });

  const options = genRegistration(user)

  req.session.challenge = options.challenge
  await req.session.save();

  return { props: { options } };
},
  sessionOptions);