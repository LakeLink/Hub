import { FormEvent, useEffect, useState } from "react";
import { withIronSessionSsr } from "iron-session/next";
import { sessionOptions } from "~/lib/session";
import { browserSupportsWebAuthn, startRegistration } from '@simplewebauthn/browser'
import { ObjectId } from "mongodb";
import mongo from "~/lib/mongo";
import { User } from "~/lib/user";
import { InferGetServerSidePropsType } from "next/types";
import { genRegistration } from "~/lib/webAuthn";

export default function Register({ options }: InferGetServerSidePropsType<typeof getServerSideProps>) {
  const [isAvailable, setIsAvailable] = useState<boolean | null>(null);

  useEffect(() => {
    const checkAvailability = async () => {
      const available =
        browserSupportsWebAuthn();
      setIsAvailable(available);
    };

    checkAvailability();
  }, []);

  const handleClick = async () => {
    console.log(options)
    let attResp = await startRegistration(options);
    console.log(attResp)
    const verificationResp = await fetch("/api/auth/webAuthn/register", {
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
        <button onClick={handleClick}>H</button>
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
  if (!user) return {
    redirect: {
      destination: '/auth/signIn',
      permanent: false
    }
  }
  const options = genRegistration(user)

  req.session.challenge = options.challenge
  await req.session.save();

  return { props: { options } };
},
  sessionOptions);